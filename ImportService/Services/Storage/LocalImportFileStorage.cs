namespace ImportService.Services.Storage;

public class LocalImportFileStorage : IImportFileStorage
{
    private readonly IWebHostEnvironment _env;

    public LocalImportFileStorage(IWebHostEnvironment env) => _env = env;

    public async Task<(string FilePath, long SizeBytes)> SaveAsync(Guid jobId, IFormFile file, CancellationToken ct)
    {
        var dir = Path.Combine(_env.ContentRootPath, "data", "imports");
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, $"{jobId}.csv");

        await using (var fs = File.Create(path))
            await file.CopyToAsync(fs, ct);

        return (path, file.Length);
    }
}