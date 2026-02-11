using Microsoft.EntityFrameworkCore;

namespace ImportService.Data;

public class ImportDbContext : DbContext
{
    public ImportDbContext(DbContextOptions<ImportDbContext> options) : base(options) { }

    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TransactionEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.BookedAt);
            b.HasIndex(x => new { x.UserId, x.BookedAt });
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