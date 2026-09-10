using Customer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = Customer.Domain.Entities.Customer;

namespace Customer.Infrastructure.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<CustomerOrderRef> CustomerOrderRefs => Set<CustomerOrderRef>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerEntity>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Name);
            entity.Property(c => c.Email).HasMaxLength(100);
            entity.Property(c => c.PhoneNumber).IsUnicode(false).HasMaxLength(30);
            entity.Property(c => c.City).HasMaxLength(50);
            entity.Property(c => c.CreatedBy).HasMaxLength(40);
            entity.Property(c => c.UpdatedBy).HasMaxLength(40);
            entity.HasMany(c => c.OrderRefs)
                .WithOne()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerOrderRef>(entity =>
        {
            entity.ToTable("CustomerOrderRefs");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.OrderId).IsUnique();
        });
    }
}
