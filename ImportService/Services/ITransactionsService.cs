using ImportService.Data;
using ImportService.Dtos;

namespace ImportService.Services;

public interface ITransactionsService
{
    Task ImportAsync(IEnumerable<TransactionImportRequest> items, CancellationToken ct);
    Task<List<TransactionEntity>> QueryAsync(string userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    Task<TransactionEntity?> GetAsync(Guid id, CancellationToken ct);
}