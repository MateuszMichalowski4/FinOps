using Microsoft.EntityFrameworkCore;

namespace ImportService.Data;

public class ImportDbContext : DbContext
{
    public ImportDbContext(DbContextOptions<ImportDbContext> options) : base(options) { }
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
}