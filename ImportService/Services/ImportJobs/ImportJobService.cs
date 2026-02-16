using ImportService.Data;
using ImportService.Data.Entities;
using ImportService.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace ImportService.Services.ImportJobs;

public class ImportJobService : IImportJobService
{
    private readonly ImportDbContext _db;
    private readonly IImportFileStorage _storage;

    public ImportJobService(ImportDbContext db, IImportFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Guid> CreateCsvImportJobAsync(string userId, string? accountId, IFormFile file, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId is required");
        if (file is null || file.Length == 0) throw new ArgumentException("file is empty");

        var job = new ImportJobEntity
        {
            UserId = userId,
            AccountId = accountId,
            Status = ImportJobStatus.Pending
        };

        var (path, size) = await _storage.SaveAsync(job.Id, file, ct);
        job.FilePath = path;
        job.FileSizeBytes = size;

        _db.ImportJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        return job.Id;
    }

    public Task<ImportJobEntity?> GetAsync(Guid id, CancellationToken ct)
        => _db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<ImportJobEntity>> ListAsync(string userId, CancellationToken ct)
        => _db.ImportJobs.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
}