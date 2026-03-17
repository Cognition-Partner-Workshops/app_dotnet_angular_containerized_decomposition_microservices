namespace Shared.Contracts.DTOs;

public record OrderDto(
    int Id,
    decimal Discount,
    string? Comments,
    string? CashierId,
    int CustomerId,
    DateTime CreatedDate,
    DateTime UpdatedDate,
    ICollection<OrderDetailDto> OrderDetails
);

public record OrderDetailDto(
    int Id,
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    int ProductId,
    int OrderId
);

public record CreateOrderDto(
    decimal Discount,
    string? Comments,
    string? CashierId,
    int CustomerId,
    ICollection<CreateOrderDetailDto> OrderDetails
);

public record CreateOrderDetailDto(
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    int ProductId
);

public record UpdateOrderDto(
    decimal Discount,
    string? Comments,
    string? CashierId,
    int CustomerId
);
