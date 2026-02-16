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
    string? AccountId = null,
    string? ExternalTransactionId = null,
    Guid? CorrelationId = null,
    int SchemaVersion = 2
);