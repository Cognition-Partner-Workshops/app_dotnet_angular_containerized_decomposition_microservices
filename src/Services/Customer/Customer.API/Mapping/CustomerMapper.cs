using Customer.API.ViewModels;
using Customer.Domain.Entities;
using CustomerEntity = Customer.Domain.Entities.Customer;

namespace Customer.API.Mapping;

public static class CustomerMapper
{
    public static CustomerVM ToViewModel(CustomerEntity customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        PhoneNumber = customer.PhoneNumber,
        Address = customer.Address,
        City = customer.City,
        Gender = customer.Gender.ToString(),
        Orders = customer.OrderRefs
            .OrderBy(r => r.OrderId)
            .Select(r => new OrderVM { Id = r.OrderId })
            .ToList()
    };

    public static IEnumerable<CustomerVM> ToViewModels(IEnumerable<CustomerEntity> customers) =>
        customers.Select(ToViewModel);

    public static CustomerEntity ToEntity(CustomerVM vm) => new()
    {
        Name = vm.Name ?? string.Empty,
        Email = vm.Email ?? string.Empty,
        PhoneNumber = vm.PhoneNumber,
        Address = vm.Address,
        City = vm.City,
        Gender = ParseGender(vm.Gender) ?? Gender.None
    };

    public static Gender? ParseGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return null;

        return Enum.TryParse<Gender>(gender, ignoreCase: true, out var parsed) ? parsed : null;
    }
}
