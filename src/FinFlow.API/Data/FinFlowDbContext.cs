using FinFlow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.API.Data;

public class FinFlowDbContext : DbContext
{
    public FinFlowDbContext(DbContextOptions<FinFlowDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasPrecision(18, 4);
            e.HasIndex(t => t.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.ProcessedAt);
        });
    }
}
