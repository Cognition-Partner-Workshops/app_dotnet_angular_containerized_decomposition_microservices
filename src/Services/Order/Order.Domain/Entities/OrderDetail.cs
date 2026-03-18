namespace Order.Domain.Entities;

public class OrderDetail
{
    public int Id { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int OrderId { get; set; }
    public OrderEntity? Order { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
