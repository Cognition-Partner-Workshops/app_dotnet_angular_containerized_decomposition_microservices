using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;

namespace Order.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEntity> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        const string priceDecimalType = "decimal(18,2)";

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Comments).HasMaxLength(500);
            entity.Property(o => o.Discount).HasColumnType(priceDecimalType);
            entity.Property(o => o.CashierId).HasMaxLength(40);
            entity.Property(o => o.CreatedBy).HasMaxLength(40);
            entity.Property(o => o.UpdatedBy).HasMaxLength(40);
            entity.HasMany(o => o.OrderDetails)
                  .WithOne(d => d.Order)
                  .HasForeignKey(d => d.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.UnitPrice).HasColumnType(priceDecimalType);
            entity.Property(d => d.Discount).HasColumnType(priceDecimalType);
            entity.Property(d => d.ProductName).HasMaxLength(100);
            entity.Property(d => d.CreatedBy).HasMaxLength(40);
            entity.Property(d => d.UpdatedBy).HasMaxLength(40);
        });
    }
}
