using Microsoft.EntityFrameworkCore;
using Product.Domain;
using ProductEntity = Product.Domain.Product;

namespace Product.Infrastructure.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.ToTable("AppProductCategories");
        });

        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.Name);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.Icon).IsUnicode(false).HasMaxLength(256);
            entity.Property(p => p.BuyingPrice).HasPrecision(18, 2);
            entity.Property(p => p.SellingPrice).HasPrecision(18, 2);
            entity.HasOne(p => p.Parent)
                .WithMany(p => p.Children)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("AppProducts");
        });
    }
}
