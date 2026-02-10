namespace CategorizationService.Data;

public class TransactionEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Merchant { get; set; } = default!;
    public DateTimeOffset BookedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}