namespace Customer.API.ViewModels;

/// <summary>
/// Order projection of the monolith's OrderVM. The Customer service only owns
/// the order identifier; Discount and Comments are owned by the Order service
/// and are left at their defaults here.
/// </summary>
public class OrderVM
{
    public int Id { get; set; }
    public decimal Discount { get; set; }
    public string? Comments { get; set; }
}
