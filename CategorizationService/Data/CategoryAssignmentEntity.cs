namespace CategorizationService.Data;

public class CategoryAssignmentEntity
{
    public Guid TransactionId { get; set; }
    public string UserId { get; set; } = default!;
    public string CategoryId { get; set; } = default!;
    public double Confidence { get; set; }

    public Guid EventId { get; set; }
    public Guid? CorrelationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}