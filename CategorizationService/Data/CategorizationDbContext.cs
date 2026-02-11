using Microsoft.EntityFrameworkCore;

namespace CategorizationService.Data;

public class CategorizationDbContext : DbContext
{
    public CategorizationDbContext(DbContextOptions<CategorizationDbContext> options) : base(options) { }

    public DbSet<CategoryAssignmentEntity> CategoryAssignments => Set<CategoryAssignmentEntity>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryAssignmentEntity>(b =>
        {
            b.HasKey(x => x.TransactionId);
            b.HasIndex(x => x.UserId);
            b.Property(x => x.CategoryId).IsRequired();
        });

        modelBuilder.Entity<ProcessedEvent>(b =>
        {
            b.HasKey(x => x.EventId);
        });
    }
}