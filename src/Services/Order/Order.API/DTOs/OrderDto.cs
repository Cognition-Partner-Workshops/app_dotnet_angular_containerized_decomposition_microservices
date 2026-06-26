namespace Order.API.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public decimal Discount { get; set; }
    public string? Comments { get; set; }
    public string? CashierId { get; set; }
    public int CustomerId { get; set; }
    public List<OrderDetailDto> OrderDetails { get; set; } = [];
}

public class CreateOrderDto
{
    public decimal Discount { get; set; }
    public string? Comments { get; set; }
    public string? CashierId { get; set; }
    public int CustomerId { get; set; }
    public List<CreateOrderDetailDto> OrderDetails { get; set; } = [];
}

public class UpdateOrderDto
{
    public decimal Discount { get; set; }
    public string? Comments { get; set; }
    public string? CashierId { get; set; }
    public int CustomerId { get; set; }
}

public class OrderDetailDto
{
    public int Id { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public int ProductId { get; set; }
}

public class CreateOrderDetailDto
{
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public int ProductId { get; set; }
}
