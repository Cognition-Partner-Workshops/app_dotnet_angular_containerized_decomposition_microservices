using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;

namespace Order.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order.Domain.Entities.Order> Orders { get; set; } = null!;
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order entity config — ported verbatim from monolith
        modelBuilder.Entity<Order.Domain.Entities.Order>(entity =>
        {
            entity.ToTable("AppOrders");
            entity.Property(o => o.Comments).HasMaxLength(500);
            entity.Property(o => o.Discount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.CreatedBy).HasMaxLength(40);
            entity.Property(o => o.UpdatedBy).HasMaxLength(40);

            entity.HasMany(o => o.OrderDetails)
                  .WithOne(od => od.Order)
                  .HasForeignKey(od => od.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderDetail entity config — ported verbatim from monolith
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("AppOrderDetails");
            entity.Property(od => od.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(od => od.Discount).HasColumnType("decimal(18,2)");
            entity.Property(od => od.CreatedBy).HasMaxLength(40);
            entity.Property(od => od.UpdatedBy).HasMaxLength(40);
        });
    }
}
