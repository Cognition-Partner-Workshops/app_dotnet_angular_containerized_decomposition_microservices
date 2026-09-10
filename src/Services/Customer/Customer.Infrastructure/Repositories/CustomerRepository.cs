using Customer.Domain.Interfaces;
using Customer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = Customer.Domain.Entities.Customer;

namespace Customer.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly CustomerDbContext _context;

    public CustomerRepository(CustomerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CustomerEntity>> GetAllCustomersDataAsync()
    {
        return await _context.Customers
            .Include(c => c.OrderRefs)
            .AsSingleQuery()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CustomerEntity>> GetTopActiveCustomersAsync(int count)
    {
        return await _context.Customers
            .Include(c => c.OrderRefs)
            .AsSingleQuery()
            .OrderByDescending(c => c.OrderRefs.Count)
            .ThenBy(c => c.Name)
            .Take(count)
            .ToListAsync();
    }

    public async Task<CustomerEntity?> GetByIdAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.OrderRefs)
            .AsSingleQuery()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CustomerEntity> AddAsync(CustomerEntity customer)
    {
        customer.CreatedDate = DateTime.UtcNow;
        customer.UpdatedDate = customer.CreatedDate;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<CustomerEntity?> UpdateAsync(int id, CustomerEntity customer)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (existing is null)
            return null;

        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.PhoneNumber = customer.PhoneNumber;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.Gender = customer.Gender;
        existing.UpdatedBy = customer.UpdatedBy;
        existing.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (existing is null)
            return false;

        _context.Customers.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
