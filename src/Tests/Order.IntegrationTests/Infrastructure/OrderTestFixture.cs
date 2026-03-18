using System.Net.Http.Json;
using Shared.Contracts.DTOs;

namespace Order.IntegrationTests.Infrastructure;

/// <summary>
/// Convenience helpers shared across test classes.
/// </summary>
public static class OrderTestFixture
{
    public static CreateOrderDto BuildCreateOrderDto(
        int customerId = 1,
        string? comments = "Test order",
        string? cashierId = null,
        decimal discount = 0m,
        int detailCount = 1)
    {
        var details = Enumerable.Range(1, detailCount)
            .Select(i => new CreateOrderDetailDto(
                UnitPrice: 10.00m * i,
                Quantity: i,
                Discount: 0m,
                ProductId: i))
            .ToList();

        return new CreateOrderDto(
            Discount: discount,
            Comments: comments,
            CashierId: cashierId,
            CustomerId: customerId,
            OrderDetails: details);
    }

    public static async Task<OrderDto> CreateOrderAsync(HttpClient client, CreateOrderDto? dto = null)
    {
        dto ??= BuildCreateOrderDto();
        var response = await client.PostAsJsonAsync("/api/order", dto);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        return order!;
    }
}
