namespace ImportService.Data.Entities;

public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}