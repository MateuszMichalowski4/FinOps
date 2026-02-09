namespace FinOps.Contracts;

public record CategoryAssigned(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TransactionId,
    string UserId,
    string CategoryId,
    double Confidence,
    int SchemaVersion = 1
);