namespace ImportService.Data.Entities;


public class TransactionEntity
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = default!;
    public string? AccountId { get; set; }

    public string? ExternalTransactionId { get; set; }

    public string? CategoryId { get; set; }
    public double? CategoryConfidence { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Merchant { get; set; } = default!;
    public DateTimeOffset BookedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}