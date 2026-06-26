using Customer.Domain.Entities;

namespace Customer.Domain.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<Entities.Customer>> GetAllCustomersAsync();
    Task<Entities.Customer?> GetCustomerByIdAsync(int id);
    Task<Entities.Customer> CreateCustomerAsync(Entities.Customer customer);
    Task<Entities.Customer?> UpdateCustomerAsync(Entities.Customer customer);
    Task<bool> DeleteCustomerAsync(int id);
}
