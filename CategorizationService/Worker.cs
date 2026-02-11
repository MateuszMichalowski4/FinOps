using System.Text.Json;
using Confluent.Kafka;
using FinOps.Contracts;
using CategorizationService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CategorizationService;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:29092";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "categorization-v1",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrap,
            Acks = Acks.All
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe("finops.transaction.imported");
        _logger.LogInformation("CategorizationService started. Listening on finops.transaction.imported");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? cr;

                try
                {
                    cr = consumer.Consume(TimeSpan.FromMilliseconds(500));
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                    continue;
                }

                if (cr?.Message?.Value is null) continue;

                TransactionImported? evt;
                try
                {
                    evt = JsonSerializer.Deserialize<TransactionImported>(cr.Message.Value);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid JSON (skipping)");
                    consumer.Commit(cr);
                    continue;
                }

                if (evt is null)
                {
                    consumer.Commit(cr);
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<CategorizationDbContext>();

                    var alreadyEvent = await db.ProcessedEvents.AnyAsync(x => x.EventId == evt.EventId, stoppingToken);
                    if (alreadyEvent)
                    {
                        consumer.Commit(cr);
                        continue;
                    }

                    var existing = await db.CategoryAssignments.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.TransactionId == evt.TransactionId, stoppingToken);

                    if (existing is not null)
                    {
                        db.ProcessedEvents.Add(new ProcessedEvent { EventId = evt.EventId });
                        await db.SaveChangesAsync(stoppingToken);

                        consumer.Commit(cr);
                        continue;
                    }

                    var (categoryId, confidence) = Categorize(evt);

                    var outEvent = new CategoryAssigned(
                        EventId: Guid.NewGuid(),
                        OccurredAt: DateTimeOffset.UtcNow,
                        TransactionId: evt.TransactionId,
                        UserId: evt.UserId,
                        CategoryId: categoryId,
                        Confidence: confidence,
                        CorrelationId: evt.CorrelationId,
                        SchemaVersion: 1
                    );

                    db.CategoryAssignments.Add(new CategoryAssignmentEntity
                    {
                        TransactionId = evt.TransactionId,
                        UserId = evt.UserId,
                        CategoryId = categoryId,
                        Confidence = confidence,
                        EventId = outEvent.EventId,
                        CorrelationId = evt.CorrelationId
                    });

                    db.ProcessedEvents.Add(new ProcessedEvent { EventId = evt.EventId });

                    await db.SaveChangesAsync(stoppingToken);

                    await producer.ProduceAsync(
                        "finops.category.assigned",
                        new Message<string, string>
                        {
                            Key = evt.UserId,
                            Value = JsonSerializer.Serialize(outEvent)
                        },
                        stoppingToken);

                    consumer.Commit(cr);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "DB conflict (idempotency). Will commit. tx={TransactionId}", evt.TransactionId);
                    consumer.Commit(cr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Processing failed; will retry. offset={Offset}", cr.Offset);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private static (string CategoryId, double Confidence) Categorize(TransactionImported evt)
    {
        var merchant = (evt.Merchant ?? "").ToLowerInvariant();

        if (merchant.Contains("zabka")) return ("groceries", 0.90);
        if (merchant.Contains("uber") || merchant.Contains("bolt")) return ("transport", 0.90);
        if (merchant.Contains("orlen")) return ("fuel", 0.90);
        if (merchant.Contains("netflix") || merchant.Contains("spotify")) return ("subscriptions", 0.85);

        return ("uncategorized", 0.50);
    }
}
