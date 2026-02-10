using System.Text.Json;
using Confluent.Kafka;
using FinOps.Contracts;
using ImportService.Data;
using Microsoft.EntityFrameworkCore;

namespace ImportService.Workers;

public class CategoryAssignedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CategoryAssignedConsumer> _logger;

    public CategoryAssignedConsumer(IServiceScopeFactory scopeFactory, ILogger<CategoryAssignedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:29092";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "import-category-updater-v1",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe("finops.category.assigned");
        _logger.LogInformation("ImportService consumer started. Listening on finops.category.assigned");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? cr = null;

                try
                {
                    // krótkie timeouty = brak wiecznego blokowania
                    cr = consumer.Consume(TimeSpan.FromMilliseconds(500));
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                    continue;
                }

                if (cr?.Message?.Value is null) continue;

                CategoryAssigned? evt;
                try
                {
                    evt = JsonSerializer.Deserialize<CategoryAssigned>(cr.Message.Value);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid JSON");
                    continue;
                }

                if (evt is null) continue;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ImportDbContext>();

                var tx = await db.Transactions.FirstOrDefaultAsync(x => x.Id == evt.TransactionId, stoppingToken);
                if (tx is null)
                {
                    _logger.LogWarning("Transaction not found. tx={TransactionId}", evt.TransactionId);
                    continue;
                }

                tx.CategoryId = evt.CategoryId;
                tx.CategoryConfidence = evt.Confidence;

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Transaction updated. tx={TransactionId} cat={CategoryId}", evt.TransactionId, evt.CategoryId);
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
