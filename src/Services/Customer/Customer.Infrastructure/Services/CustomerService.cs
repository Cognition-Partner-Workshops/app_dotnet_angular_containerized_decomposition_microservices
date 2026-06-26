using Customer.Domain.Entities;
using Customer.Domain.Interfaces;
using Customer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Customer.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly CustomerDbContext _dbContext;

    public CustomerService(CustomerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Domain.Entities.Customer>> GetAllCustomersAsync()
    {
        return await _dbContext.Customers
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Domain.Entities.Customer?> GetCustomerByIdAsync(int id)
    {
        return await _dbContext.Customers.FindAsync(id);
    }

    public async Task<Domain.Entities.Customer> CreateCustomerAsync(Domain.Entities.Customer customer)
    {
        customer.CreatedDate = DateTime.UtcNow;
        customer.UpdatedDate = DateTime.UtcNow;
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();
        return customer;
    }

    public async Task<Domain.Entities.Customer?> UpdateCustomerAsync(Domain.Entities.Customer customer)
    {
        var existing = await _dbContext.Customers.FindAsync(customer.Id);
        if (existing == null) return null;

        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.PhoneNumber = customer.PhoneNumber;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.Gender = customer.Gender;
        existing.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null) return false;

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
