namespace Shared.Contracts.DTOs;

public record OrderDto(
    int Id,
    int CustomerId,
    string? CashierId,
    decimal Discount,
    string? Comments,
    DateTime CreatedDate,
    DateTime UpdatedDate,
    IReadOnlyList<OrderDetailDto> OrderDetails
);

public record OrderDetailDto(
    int Id,
    int ProductId,
    string? ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Discount
);

public record CreateOrderRequest(
    int CustomerId,
    string? CashierId,
    decimal Discount,
    string? Comments,
    IReadOnlyList<CreateOrderDetailRequest> OrderDetails
);

public record CreateOrderDetailRequest(
    int ProductId,
    string? ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Discount
);

public record UpdateOrderRequest(
    decimal Discount,
    string? Comments
);
