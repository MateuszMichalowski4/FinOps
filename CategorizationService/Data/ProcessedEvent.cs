namespace CategorizationService.Data;

public class ProcessedEvent
{
    public Guid EventId { get; set; } 
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}