using Microsoft.EntityFrameworkCore;
using Entities = Customer.Domain.Entities;

namespace Customer.Infrastructure.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<Entities.Customer> Customers => Set<Entities.Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // NOTE: Use `Entities.Customer` to disambiguate from the root `Customer` namespace
        modelBuilder.Entity<Entities.Customer>(entity =>
        {
            entity.ToTable("AppCustomers");
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Name);
            entity.Property(c => c.Email).HasMaxLength(100);
            entity.Property(c => c.PhoneNumber).IsUnicode(false).HasMaxLength(30);
            entity.Property(c => c.City).HasMaxLength(50);
            entity.Property(c => c.CreatedBy).HasMaxLength(40);
            entity.Property(c => c.UpdatedBy).HasMaxLength(40);
        });
    }
}
