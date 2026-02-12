namespace ImportService.Dtos;

public record TransactionImportRequest(
    string UserId,
    decimal Amount,
    string Currency,
    string Merchant,
    DateTimeOffset BookedAt
);