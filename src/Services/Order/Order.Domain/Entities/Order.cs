namespace Order.Domain.Entities;

/// <summary>
/// An order placed by a customer. <see cref="CustomerId"/> and <see cref="CashierId"/>
/// point at records owned by the Customer and Identity services; they are plain ids,
/// not validated by this database.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public decimal Discount { get; set; }
    public string? Comments { get; set; }

    public string? CashierId { get; set; }
    public int CustomerId { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; } = [];
}
