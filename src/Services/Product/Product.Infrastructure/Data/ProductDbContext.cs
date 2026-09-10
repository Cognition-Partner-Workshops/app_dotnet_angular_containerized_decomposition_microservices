using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using ProductEntity = Product.Domain.Entities.Product;

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
            entity.ToTable("AppProductCategories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.Property(c => c.Icon).HasMaxLength(256);
            entity.Property(c => c.CreatedBy).HasMaxLength(40);
            entity.Property(c => c.UpdatedBy).HasMaxLength(40);
        });

        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.ToTable("AppProducts");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.Name);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.Icon).HasMaxLength(256);
            entity.Property(p => p.CreatedBy).HasMaxLength(40);
            entity.Property(p => p.UpdatedBy).HasMaxLength(40);
            entity.Property(p => p.BuyingPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.SellingPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(p => p.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.ProductCategory)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.ProductCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AddAuditInfo();
        return base.SaveChanges();
    }

    private void AddAuditInfo()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.UpdatedDate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedDate = now;
            }
        }
    }
}
