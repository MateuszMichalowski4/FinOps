using System.Text.Json;
using Confluent.Kafka;
using FinOps.Contracts;
using ImportService.Handlers;

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

        var cfg = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "import-category-updater-v1",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(cfg).Build();
        consumer.Subscribe("finops.category.assigned");

        _logger.LogInformation("Listening on finops.category.assigned");

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

                CategoryAssigned? evt;
                try
                {
                    evt = JsonSerializer.Deserialize<CategoryAssigned>(cr.Message.Value);
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
                    var handler = scope.ServiceProvider.GetRequiredService<CategoryAssignedHandler>();

                    await handler.HandleAsync(evt, stoppingToken);

                    consumer.Commit(cr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler failed; will retry. offset={Offset}", cr.Offset);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
