using Microsoft.EntityFrameworkCore;
using Customer.Domain.Interfaces;
using Customer.Infrastructure.Data;
using Entities = Customer.Domain.Entities;

namespace Customer.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly CustomerDbContext _context;

    public CustomerService(CustomerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Entities.Customer>> GetAllAsync()
    {
        return await _context.Customers.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Entities.Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<Entities.Customer> CreateAsync(Entities.Customer customer)
    {
        customer.CreatedDate = DateTime.UtcNow;
        customer.UpdatedDate = DateTime.UtcNow;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateAsync(Entities.Customer customer)
    {
        customer.UpdatedDate = DateTime.UtcNow;
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return false;
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }
}
