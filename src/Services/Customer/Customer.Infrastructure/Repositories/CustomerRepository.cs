using Microsoft.EntityFrameworkCore;
using Customer.Domain.Interfaces;
using Customer.Infrastructure.Data;

namespace Customer.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly CustomerDbContext _context;

    public CustomerRepository(CustomerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Domain.Entities.Customer>> GetAllAsync()
    {
        return await _context.Customers
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Domain.Entities.Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<Domain.Entities.Customer> AddAsync(Domain.Entities.Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateAsync(Domain.Entities.Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Customers.FindAsync(id);
        if (entity is not null)
        {
            _context.Customers.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
