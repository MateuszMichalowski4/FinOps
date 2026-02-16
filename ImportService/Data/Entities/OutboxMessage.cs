namespace ImportService.Data.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string Topic { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string? HeadersJson { get; set; }

    public int SchemaVersion { get; set; } = 1;

    public int Attempts { get; set; } = 0;
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastError { get; set; }
}