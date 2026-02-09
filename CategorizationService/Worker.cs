using System;
using Confluent.Kafka;
using FinOps.Contracts;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CategorizationService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger) => _logger = logger;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
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

        return Task.Run(() =>
        {
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            consumer.Subscribe("finops.transaction.imported");
            _logger.LogInformation("CategorizationService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);

                    var imported = JsonSerializer.Deserialize<TransactionImported>(cr.Message.Value);
                    if (imported is null)
                    {
                        _logger.LogWarning("Invalid message: {Value}", cr.Message.Value);
                        continue;
                    }

                    var assigned = new CategoryAssigned(
                        EventId: Guid.NewGuid(),
                        OccurredAt: DateTimeOffset.UtcNow,
                        TransactionId: imported.TransactionId,
                        UserId: imported.UserId,
                        CategoryId: "groceries",
                        Confidence: 0.90
                    );

                    producer.Produce(
                        "finops.transaction.categorized",
                        new Message<string, string>
                        {
                            Key = assigned.UserId,
                            Value = JsonSerializer.Serialize(assigned)
                        });

                    _logger.LogInformation("Assigned category for tx={TxId}", imported.TransactionId);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consume/publish error");
                    Thread.Sleep(1000);
                }
            }

            consumer.Close();
        }, stoppingToken);
    }
}
