using System.Text.Json;
using Confluent.Kafka;
using FinOps.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CategorizationService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
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
            EnableAutoCommit = true
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
                var cr = consumer.Consume(stoppingToken);

                var evt = JsonSerializer.Deserialize<TransactionImported>(cr.Message.Value);
                if (evt is null) continue;

                var (categoryId, confidence) = Categorize(evt);

                var outEvent = new CategoryAssigned(
                    EventId: Guid.NewGuid(),
                    OccurredAt: DateTimeOffset.UtcNow,
                    TransactionId: evt.TransactionId,
                    UserId: evt.UserId,
                    CategoryId: categoryId,
                    Confidence: confidence,
                    SchemaVersion: 1
                );

                var json = JsonSerializer.Serialize(outEvent);

                await producer.ProduceAsync(
                    "finops.category.assigned",
                    new Message<string, string>
                    {
                        Key = evt.UserId,
                        Value = json
                    },
                    stoppingToken);

                _logger.LogInformation(
                    "CategoryAssigned published. tx={TransactionId} user={UserId} cat={CategoryId} conf={Confidence}",
                    evt.TransactionId, evt.UserId, categoryId, confidence);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
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
        if (merchant.Contains("netflix") || merchant.Contains("spotify")) return ("subscriptions", 0.85);

        return ("uncategorized", 0.50);
    }
}
