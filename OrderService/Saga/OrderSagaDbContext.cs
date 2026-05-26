using Microsoft.EntityFrameworkCore;

namespace OrderService.Saga;

public class OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options) : DbContext(options)
{
    public DbSet<OrderSagaState> OrderSagaStates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderSagaState>(entity =>
        {
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.ProductName).HasMaxLength(256);
            entity.Property(x => x.CustomerEmail).HasMaxLength(256);
        });
    }
}
