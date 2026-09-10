namespace Customer.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public Gender Gender { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    /// <summary>
    /// Orders placed by this customer, referenced by identifier only. Order data
    /// itself is owned by the Order service.
    /// </summary>
    public ICollection<CustomerOrderRef> OrderRefs { get; } = [];
}
