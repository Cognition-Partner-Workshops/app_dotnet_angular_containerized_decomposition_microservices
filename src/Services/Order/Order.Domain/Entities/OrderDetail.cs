namespace Order.Domain.Entities;

// Line item within the Order bounded context.
// The catalog context is referenced by id only (ProductId); its navigation from the monolith is intentionally dropped.
public class OrderDetail : BaseEntity
{
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }

    public int ProductId { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }
}
