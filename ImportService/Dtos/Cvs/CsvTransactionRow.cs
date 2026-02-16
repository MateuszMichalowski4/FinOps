namespace ImportService.Dtos.Csv;

public class CsvTransactionRow
{
    public string ExternalId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Merchant { get; set; } = default!;
    public DateTimeOffset BookedAt { get; set; }
}