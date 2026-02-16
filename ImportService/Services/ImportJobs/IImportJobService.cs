using ImportService.Data.Entities;

namespace ImportService.Services.ImportJobs;

public interface IImportJobService
{
    Task<Guid> CreateCsvImportJobAsync(string userId, string? accountId, IFormFile file, CancellationToken ct);
    Task<ImportJobEntity?> GetAsync(Guid id, CancellationToken ct);
    Task<List<ImportJobEntity>> ListAsync(string userId, CancellationToken ct);
}