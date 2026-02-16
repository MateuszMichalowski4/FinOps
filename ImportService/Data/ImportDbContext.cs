using ImportService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImportService.Data;

public class ImportDbContext : DbContext
{
    public ImportDbContext(DbContextOptions<ImportDbContext> options) : base(options) { }

    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<ImportJobEntity> ImportJobs => Set<ImportJobEntity>(); // NEW

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TransactionEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.BookedAt);
            b.HasIndex(x => new { x.UserId, x.BookedAt });

            b.HasIndex(x => new { x.UserId, x.ExternalTransactionId })
                .IsUnique()
                .HasFilter("\"ExternalTransactionId\" IS NOT NULL");
        });

        modelBuilder.Entity<ImportJobEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.CreatedAt);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.FilePath).IsRequired();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.SentAt);
            b.HasIndex(x => x.NextAttemptAt);
            b.Property(x => x.Topic).IsRequired();
            b.Property(x => x.Key).IsRequired();
            b.Property(x => x.Payload).IsRequired();
        });

        modelBuilder.Entity<ProcessedEvent>(b =>
        {
            b.HasKey(x => x.EventId);
        });
    }
}