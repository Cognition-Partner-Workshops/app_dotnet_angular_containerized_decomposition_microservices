using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;

namespace Product.Infrastructure.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product.Domain.Entities.Product> Products => Set<Product.Domain.Entities.Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ProductCategory — verbatim from monolith
        builder.Entity<ProductCategory>().Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Entity<ProductCategory>().Property(p => p.Description).HasMaxLength(500);
        builder.Entity<ProductCategory>().ToTable("AppProductCategories");

        // Product — verbatim from monolith
        builder.Entity<Product.Domain.Entities.Product>().Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Entity<Product.Domain.Entities.Product>().HasIndex(p => p.Name);
        builder.Entity<Product.Domain.Entities.Product>().Property(p => p.Description).HasMaxLength(500);
        builder.Entity<Product.Domain.Entities.Product>().Property(p => p.Icon).IsUnicode(false).HasMaxLength(256);
        builder.Entity<Product.Domain.Entities.Product>().HasOne(p => p.Parent).WithMany(p => p.Children).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Product.Domain.Entities.Product>().Property(p => p.BuyingPrice).HasColumnType("decimal(18,2)");
        builder.Entity<Product.Domain.Entities.Product>().Property(p => p.SellingPrice).HasColumnType("decimal(18,2)");
        builder.Entity<Product.Domain.Entities.Product>().ToTable("AppProducts");
    }
}
