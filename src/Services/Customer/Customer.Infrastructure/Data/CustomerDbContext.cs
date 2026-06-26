using Microsoft.EntityFrameworkCore;
using CustomerEntity = Customer.Domain.Entities.Customer;

namespace Customer.Infrastructure.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerEntity>().Property(c => c.Name).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<CustomerEntity>().HasIndex(c => c.Name);
        modelBuilder.Entity<CustomerEntity>().Property(c => c.Email).HasMaxLength(100);
        modelBuilder.Entity<CustomerEntity>().Property(c => c.PhoneNumber).IsUnicode(false).HasMaxLength(30);
        modelBuilder.Entity<CustomerEntity>().Property(c => c.City).HasMaxLength(50);
        modelBuilder.Entity<CustomerEntity>().ToTable("AppCustomers");
    }
}
