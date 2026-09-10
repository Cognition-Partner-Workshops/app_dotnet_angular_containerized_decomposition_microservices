namespace Customer.API.ViewModels;

/// <summary>
/// Wire shape carried over unchanged from the monolith's CustomerVM so existing
/// clients can be repointed at the gateway without changes.
/// </summary>
public class CustomerVM
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Gender { get; set; }

    public ICollection<OrderVM>? Orders { get; set; }
}
