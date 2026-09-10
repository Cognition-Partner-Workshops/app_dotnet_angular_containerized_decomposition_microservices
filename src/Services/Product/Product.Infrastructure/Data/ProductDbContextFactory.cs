using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Product.Infrastructure.Data;

/// <summary>
/// Design-time factory used by `dotnet ef` to create migrations without booting the API.
/// </summary>
public class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=productdb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ProductDbContext(options);
    }
}
