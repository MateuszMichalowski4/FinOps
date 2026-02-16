using CsvHelper;
using CsvHelper.Configuration;
using FinOps.Contracts;
using ImportService.Data;
using ImportService.Data.Entities;
using ImportService.Dtos.Csv;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;

namespace ImportService.Workers;

public class ImportJobWorker : BackgroundService
{
    private const int BatchSize = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportJobWorker> _logger;

    public ImportJobWorker(IServiceScopeFactory scopeFactory, ILogger<ImportJobWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ImportDbContext>();

            var job = await db.ImportJobs
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.Status == ImportJobStatus.Pending, stoppingToken);

            if (job is null)
            {
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            await ProcessJobAsync(job.Id, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ImportDbContext>();

        var job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);

        job.Status = ImportJobStatus.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var correlationId = job.Id;

        try
        {
            using var stream = File.OpenRead(job.FilePath);
            using var reader = new StreamReader(stream);

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, cfg);
            
            csv.Context.RegisterClassMap<CsvTransactionRowMap>();

            var batch = new List<CsvTransactionRow>(BatchSize);

            await foreach (var row in csv.GetRecordsAsync<CsvTransactionRow>(ct))
            {
                batch.Add(row);
                if (batch.Count >= BatchSize)
                {
                    await ProcessBatchAsync(jobId, batch, correlationId, ct);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await ProcessBatchAsync(jobId, batch, correlationId, ct);

            // refresh + finalize
            job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);
            job.Status = ImportJobStatus.Completed;
            job.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);
            job.Status = ImportJobStatus.Failed;
            job.Error = ex.Message;
            job.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            _logger.LogError(ex, "Import job failed. jobId={JobId}", jobId);
        }
    }

    private async Task ProcessBatchAsync(Guid jobId, List<CsvTransactionRow> batch, Guid correlationId, CancellationToken ct)
{
    using var scope = _scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ImportDbContext>();

    var job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);

    job.Total += batch.Count;

    // collect externalIds (trim + non-empty)
    var incoming = batch
        .Select(r => r.ExternalId?.Trim())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .ToList()!;

    // rows without externalId -> failed
    var invalidCount = batch.Count - incoming.Count;
    if (invalidCount > 0) job.Failed += invalidCount;

    // detect duplicates within the same CSV batch
    var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var uniqueIncoming = new List<string>(incoming.Count);
    foreach (var extId in incoming)
    {
        if (seenInBatch.Add(extId!)) uniqueIncoming.Add(extId!);
        else job.Skipped++; 
    }

    // pre-load existing externalIds from DB for this user (single query)
    var existingExternalIds = await db.Transactions.AsNoTracking()
        .Where(t => t.UserId == job.UserId
                    && t.ExternalTransactionId != null
                    && uniqueIncoming.Contains(t.ExternalTransactionId))
        .Select(t => t.ExternalTransactionId!)
        .ToListAsync(ct);

    var existingSet = new HashSet<string>(existingExternalIds, StringComparer.OrdinalIgnoreCase);

    foreach (var row in batch)
    {
        var extId = row.ExternalId?.Trim();
        if (string.IsNullOrWhiteSpace(extId)) continue; 

        if (!seenInBatch.Contains(extId)) continue;
        
    }

    var firstOccurrence = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var row in batch)
    {
        var extId = row.ExternalId?.Trim();
        if (string.IsNullOrWhiteSpace(extId)) continue;

        if (!firstOccurrence.Add(extId))
            continue;

        if (existingSet.Contains(extId))
        {
            job.Skipped++;
            continue;
        }

        var txId = Guid.NewGuid();

        db.Transactions.Add(new ImportService.Data.Entities.TransactionEntity
        {
            Id = txId,
            UserId = job.UserId,
            AccountId = job.AccountId,
            ExternalTransactionId = extId,
            Amount = row.Amount,
            Currency = row.Currency,
            Merchant = row.Merchant ?? "",
            BookedAt = row.BookedAt,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var evt = new TransactionImported(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            TransactionId: txId,
            UserId: job.UserId,
            Amount: row.Amount,
            Currency: row.Currency,
            Merchant: row.Merchant ?? "",
            BookedAt: row.BookedAt,
            AccountId: job.AccountId,
            ExternalTransactionId: extId,
            CorrelationId: correlationId,
            SchemaVersion: 2
        );

        var headers = new Dictionary<string, string>
        {
            [EventHeaders.EventId] = evt.EventId.ToString(),
            [EventHeaders.CorrelationId] = correlationId.ToString(),
            [EventHeaders.SchemaVersion] = evt.SchemaVersion.ToString(),
            [EventHeaders.Service] = "import-service"
        };

        db.Outbox.Add(new OutboxMessage
        {
            OccurredAt = evt.OccurredAt,
            Topic = Topics.TransactionImported,
            Key = job.UserId,
            Payload = Json.Serialize(evt),
            HeadersJson = Json.Serialize(headers),
            SchemaVersion = evt.SchemaVersion
        });

        job.Success++;
    }

    try
    {
        await db.SaveChangesAsync(ct);
        job.Processed += batch.Count;
        await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
    {
        // should be rare now (race), treat as skipped and continue
        db.ChangeTracker.Clear();
        job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);
        job.Skipped++;
        job.Processed += batch.Count;
        await db.SaveChangesAsync(ct);
    }
}

    private sealed class CsvTransactionRowMap : ClassMap<CsvTransactionRow>
    {
        public CsvTransactionRowMap()
        {
            Map(m => m.ExternalId).Name("externalId", "external_id", "id");
            Map(m => m.Amount).Name("amount");
            Map(m => m.Currency).Name("currency");
            Map(m => m.Merchant).Name("merchant");
            Map(m => m.BookedAt).Name("bookedAt", "booked_at");
        }
    }
}