using Customer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Customer.Infrastructure.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Entities.Customer> Customers => Set<Domain.Entities.Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.Entities.Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.Address);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.Gender);

            entity.Property(e => e.CreatedBy).HasMaxLength(40);
            entity.Property(e => e.UpdatedBy).HasMaxLength(40);
            entity.Property(e => e.CreatedDate);
            entity.Property(e => e.UpdatedDate);

            entity.HasIndex(e => e.Name);

            entity.ToTable("AppCustomers");
        });
    }
}
