using System.Text.Json;
using ImportService.Data;
using ImportService.Kafka;
using Microsoft.EntityFrameworkCore;

namespace ImportService.Workers;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ImportDbContext>();
                var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

                var now = DateTimeOffset.UtcNow;

                var batch = await db.Outbox
                    .Where(x => x.SentAt == null && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
                    .OrderBy(x => x.CreatedAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                foreach (var msg in batch)
                {
                    try
                    {
                        var headers = msg.HeadersJson is null
                            ? null
                            : JsonSerializer.Deserialize<Dictionary<string, string>>(msg.HeadersJson);

                        await producer.PublishAsync(msg.Topic, msg.Key, msg.Payload, headers, stoppingToken);

                        msg.SentAt = DateTimeOffset.UtcNow;
                        msg.LastError = null;
                    }
                    catch (Exception ex)
                    {
                        msg.Attempts++;
                        msg.LastError = ex.Message;

                        var seconds = Math.Min(300, (int)Math.Pow(2, Math.Min(10, msg.Attempts)));
                        msg.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(seconds);

                        _logger.LogError(ex, "Outbox publish failed. id={OutboxId} attempt={Attempts}", msg.Id, msg.Attempts);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisher loop error");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
