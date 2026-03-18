namespace Order.Domain.Entities;

public class OrderEntity
{
    public int Id { get; set; }
    public decimal Discount { get; set; }
    public string? Comments { get; set; }
    public string? CashierId { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; } = [];
}
