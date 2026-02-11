using System;

namespace FinOps.Contracts;

public record TransactionImported(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TransactionId,
    string UserId,
    decimal Amount,
    string Currency,
    string Merchant,
    DateTimeOffset BookedAt,
    Guid? CorrelationId = null,
    int SchemaVersion = 1
);