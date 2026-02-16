using System.Text.Json;
using FinOps.Contracts;
using ImportService.Data;
using ImportService.Data.Entities;
using ImportService.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ImportService.Services;

public class TransactionsService : ITransactionsService
{
    private readonly ImportDbContext _db;

    public TransactionsService(ImportDbContext db)
    {
        _db = db;
    }

    public async Task ImportAsync(IEnumerable<TransactionImportRequest> items, CancellationToken ct)
    {
        var correlationId = Guid.NewGuid();

        foreach (var item in items)
        {
            var txId = Guid.NewGuid();

            _db.Transactions.Add(new TransactionEntity
            {
                Id = txId,
                UserId = item.UserId,
                Amount = item.Amount,
                Currency = item.Currency,
                Merchant = item.Merchant,
                BookedAt = item.BookedAt,
                CreatedAt = DateTimeOffset.UtcNow
            });

            var evt = new TransactionImported(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                TransactionId: txId,
                UserId: item.UserId,
                Amount: item.Amount,
                Currency: item.Currency,
                Merchant: item.Merchant,
                BookedAt: item.BookedAt,
                CorrelationId: correlationId,
                SchemaVersion: 1
            );

            var headers = new Dictionary<string, string>
            {
                [EventHeaders.EventId] = evt.EventId.ToString(),
                [EventHeaders.CorrelationId] = correlationId.ToString(),
                [EventHeaders.SchemaVersion] = evt.SchemaVersion.ToString(),
                [EventHeaders.Service] = "import-service"
            };

            _db.Outbox.Add(new OutboxMessage
            {
                OccurredAt = evt.OccurredAt,
                Topic = Topics.TransactionImported,
                Key = item.UserId,
                Payload = Json.Serialize(evt),
                HeadersJson = Json.Serialize(headers),
                SchemaVersion = evt.SchemaVersion
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<List<TransactionEntity>> QueryAsync(string userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var q = _db.Transactions.AsNoTracking().Where(x => x.UserId == userId);

        if (from.HasValue) q = q.Where(x => x.BookedAt >= from.Value);
        if (to.HasValue)   q = q.Where(x => x.BookedAt <= to.Value);

        return q.OrderByDescending(x => x.BookedAt).ToListAsync(ct);
    }

    public Task<TransactionEntity?> GetAsync(Guid id, CancellationToken ct)
        => _db.Transactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
}
