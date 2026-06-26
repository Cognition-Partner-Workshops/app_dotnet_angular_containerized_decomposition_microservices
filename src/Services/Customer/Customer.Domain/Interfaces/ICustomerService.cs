namespace Customer.Domain.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<Entities.Customer>> GetAllAsync();
    Task<Entities.Customer?> GetByIdAsync(int id);
    Task<Entities.Customer> CreateAsync(Entities.Customer customer);
    Task UpdateAsync(Entities.Customer customer);
    Task<bool> DeleteAsync(int id);
}
