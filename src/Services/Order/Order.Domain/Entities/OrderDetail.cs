namespace Order.Domain.Entities;

/// <summary>
/// A line on an <see cref="Order"/>. <see cref="ProductId"/> points at a record owned by
/// the Product service; it is a plain id, not validated by this database.
/// </summary>
public class OrderDetail
{
    public int Id { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }

    public int ProductId { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
