using CustomerEntity = Customer.Domain.Entities.Customer;

namespace Customer.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<IReadOnlyList<CustomerEntity>> GetAllCustomersDataAsync();
    Task<IReadOnlyList<CustomerEntity>> GetTopActiveCustomersAsync(int count);
    Task<CustomerEntity?> GetByIdAsync(int id);
    Task<CustomerEntity> AddAsync(CustomerEntity customer);
    Task<CustomerEntity?> UpdateAsync(int id, CustomerEntity customer);
    Task<bool> DeleteAsync(int id);
}
