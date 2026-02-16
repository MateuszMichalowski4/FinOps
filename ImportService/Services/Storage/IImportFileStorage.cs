namespace ImportService.Services.Storage;

public interface IImportFileStorage
{
    Task<(string FilePath, long SizeBytes)> SaveAsync(Guid jobId, IFormFile file, CancellationToken ct);
}