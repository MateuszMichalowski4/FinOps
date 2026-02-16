namespace ImportService.Data.Entities;

public class ImportJobEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public string? AccountId { get; set; }

    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;

    public int Total { get; set; }
    public int Processed { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    public string FilePath { get; set; } = default!;
    public long FileSizeBytes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public string? Error { get; set; }

    public byte[] RowVersion { get; set; } = default!;
}