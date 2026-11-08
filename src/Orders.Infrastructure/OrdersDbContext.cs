// OrdersDbContext.cs
using Microsoft.EntityFrameworkCore;
using Orders.Domain;

namespace Orders.Infrastructure;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("Orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.Total).HasPrecision(18, 2);
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            order.HasIndex(o => o.CustomerId);
        });
    }
}
