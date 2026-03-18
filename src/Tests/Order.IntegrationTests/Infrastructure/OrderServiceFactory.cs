using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.Infrastructure.Data;

namespace Order.IntegrationTests.Infrastructure;

/// <summary>
/// Shared WebApplicationFactory that wires the Order microservice with an
/// isolated in-memory database so tests never touch PostgreSQL.
/// </summary>
public class OrderServiceFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"OrderTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove all DbContextOptions registrations so we can swap Npgsql → InMemory
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.FullName != null &&
                            d.ServiceType.FullName.Contains("DbContextOptions"))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // Remove the DbContext itself so we can re-register it
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(OrderDbContext));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            services.AddDbContext<OrderDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
