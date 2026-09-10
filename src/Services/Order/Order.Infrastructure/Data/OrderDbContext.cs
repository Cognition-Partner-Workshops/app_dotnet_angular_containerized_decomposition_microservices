using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderEntity>(order =>
        {
            order.ToTable("Orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.Discount).HasPrecision(18, 2);
            order.Property(o => o.Comments).HasMaxLength(500);
            order.Property(o => o.CashierId).HasMaxLength(450);
            order.Property(o => o.CreatedBy).HasMaxLength(40);
            order.Property(o => o.UpdatedBy).HasMaxLength(40);
            order.HasIndex(o => o.CustomerId);

            order.HasMany(o => o.OrderDetails)
                .WithOne(d => d.Order)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderDetail>(detail =>
        {
            detail.ToTable("OrderDetails");
            detail.HasKey(d => d.Id);
            detail.Property(d => d.UnitPrice).HasPrecision(18, 2);
            detail.Property(d => d.Discount).HasPrecision(18, 2);
            detail.Property(d => d.CreatedBy).HasMaxLength(40);
            detail.Property(d => d.UpdatedBy).HasMaxLength(40);
            detail.HasIndex(d => d.ProductId);
        });
    }
}
