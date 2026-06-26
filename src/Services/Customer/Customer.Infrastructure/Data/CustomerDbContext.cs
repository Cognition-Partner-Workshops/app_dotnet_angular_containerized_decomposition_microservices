using Customer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Customer.Infrastructure.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Entities.Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        const string tablePrefix = "App";

        builder.Entity<Domain.Entities.Customer>().Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Entity<Domain.Entities.Customer>().HasIndex(c => c.Name);
        builder.Entity<Domain.Entities.Customer>().Property(c => c.Email).HasMaxLength(100);
        builder.Entity<Domain.Entities.Customer>().Property(c => c.PhoneNumber).IsUnicode(false).HasMaxLength(30);
        builder.Entity<Domain.Entities.Customer>().Property(c => c.City).HasMaxLength(50);
        builder.Entity<Domain.Entities.Customer>().ToTable($"{tablePrefix}Customers");
    }
}
