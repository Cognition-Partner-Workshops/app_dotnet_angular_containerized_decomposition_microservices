using Customer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = Customer.Domain.Entities.Customer;

namespace Customer.Infrastructure.Data;

/// <summary>
/// Seeds the demo customers the monolith's DatabaseSeeder created, so the
/// decomposed service returns the same read-path data.
/// </summary>
public static class CustomerDbSeeder
{
    public static async Task SeedAsync(CustomerDbContext context)
    {
        if (await context.Customers.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var cust1 = new CustomerEntity
        {
            Name = "Ebenezer Monney",
            Email = "contact@ebenmonney.com",
            Gender = Gender.Male,
            CreatedDate = now,
            UpdatedDate = now
        };

        var cust2 = new CustomerEntity
        {
            Name = "Itachi Uchiha",
            Email = "uchiha@narutoverse.com",
            PhoneNumber = "+81123456789",
            Address = "Some fictional Address, Street 123, Konoha",
            City = "Konoha",
            Gender = Gender.Male,
            CreatedDate = now,
            UpdatedDate = now
        };

        var cust3 = new CustomerEntity
        {
            Name = "John Doe",
            Email = "johndoe@anonymous.com",
            PhoneNumber = "+18585858",
            Address = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec odio.
                    Praesent libero. Sed cursus ante dapibus diam. Sed nisi. Nulla quis sem at elementum imperdiet",
            City = "Lorem Ipsum",
            Gender = Gender.Male,
            CreatedDate = now,
            UpdatedDate = now
        };

        var cust4 = new CustomerEntity
        {
            Name = "Jane Doe",
            Email = "Janedoe@anonymous.com",
            PhoneNumber = "+18585858",
            Address = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec odio.
                    Praesent libero. Sed cursus ante dapibus diam. Sed nisi. Nulla quis sem at elementum imperdiet",
            City = "Lorem Ipsum",
            Gender = Gender.Male,
            CreatedDate = now,
            UpdatedDate = now
        };

        cust1.OrderRefs.Add(new CustomerOrderRef { OrderId = 1 });
        cust2.OrderRefs.Add(new CustomerOrderRef { OrderId = 2 });

        context.Customers.AddRange(cust1, cust2, cust3, cust4);
        await context.SaveChangesAsync();
    }
}
