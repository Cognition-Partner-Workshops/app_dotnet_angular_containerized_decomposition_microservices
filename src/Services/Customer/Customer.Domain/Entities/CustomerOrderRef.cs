namespace Customer.Domain.Entities;

public class CustomerOrderRef
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int OrderId { get; set; }
}
