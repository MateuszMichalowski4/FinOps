using Microsoft.EntityFrameworkCore;

namespace CategorizationService.Data;

public class FinOpsDbContext : DbContext
{
    public FinOpsDbContext(DbContextOptions<FinOpsDbContext> options) : base(options) { }

    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
}