using Customer.Domain.Entities;

namespace Customer.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Entities.Customer>> GetAllAsync();
    Task<Entities.Customer?> GetByIdAsync(int id);
    Task<Entities.Customer> AddAsync(Entities.Customer customer);
    Task UpdateAsync(Entities.Customer customer);
    Task DeleteAsync(int id);
}
