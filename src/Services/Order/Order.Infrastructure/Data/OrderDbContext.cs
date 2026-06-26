using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;

namespace Order.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order.Domain.Entities.Order> Orders => Set<Order.Domain.Entities.Order>();

    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ported verbatim from the monolith's ApplicationDbContext for the members kept in this context.
        modelBuilder.Entity<Order.Domain.Entities.Order>().Property(o => o.Comments).HasMaxLength(500);
        modelBuilder.Entity<Order.Domain.Entities.Order>().Property(o => o.Discount).HasPrecision(18, 2);
        modelBuilder.Entity<Order.Domain.Entities.Order>().ToTable("AppOrders");

        modelBuilder.Entity<OrderDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderDetail>().Property(d => d.Discount).HasPrecision(18, 2);
        modelBuilder.Entity<OrderDetail>().ToTable("AppOrderDetails");

        // Relationship within this bounded context (Order <-> OrderDetail).
        // Relationships to other contexts are intentionally NOT configured — that data lives in
        // other services and is referenced here by id only (CustomerId / ProductId).
        modelBuilder.Entity<OrderDetail>()
            .HasOne(d => d.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
