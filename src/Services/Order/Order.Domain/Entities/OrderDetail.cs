namespace Order.Domain.Entities;

public class OrderDetail : BaseEntity
{
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public int ProductId { get; set; }
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;
}
