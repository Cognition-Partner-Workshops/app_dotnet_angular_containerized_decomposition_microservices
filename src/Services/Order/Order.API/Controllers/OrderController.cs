using Microsoft.AspNetCore.Mvc;
using Order.Domain.Entities;
using Order.Domain.Interfaces;
using Shared.Contracts.DTOs;

using OrderEntity = Order.Domain.Entities.OrderEntity;

namespace Order.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllOrdersAsync(cancellationToken);
        return Ok(orders.Select(MapToDto));
    }

    [HttpGet("customer/{customerId:int}")]
    public async Task<IActionResult> GetByCustomer(int customerId, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersByCustomerAsync(customerId, cancellationToken);
        return Ok(orders.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound();
        return Ok(MapToDto(order));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = new OrderEntity
        {
            CustomerId = request.CustomerId,
            CashierId = request.CashierId,
            Discount = request.Discount,
            Comments = request.Comments
        };

        foreach (var d in request.OrderDetails)
        {
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = d.ProductId,
                ProductName = d.ProductName,
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                Discount = d.Discount
            });
        }

        var created = await _orderService.CreateOrderAsync(order, cancellationToken);
        _logger.LogInformation("Created order {OrderId} for customer {CustomerId}", created.Id, created.CustomerId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        var existing = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        existing.Discount = request.Discount;
        existing.Comments = request.Comments;

        var updated = await _orderService.UpdateOrderAsync(existing, cancellationToken);
        return Ok(MapToDto(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        await _orderService.DeleteOrderAsync(id, cancellationToken);
        return NoContent();
    }

    private static OrderDto MapToDto(OrderEntity o) => new(
        o.Id,
        o.CustomerId,
        o.CashierId,
        o.Discount,
        o.Comments,
        o.CreatedDate,
        o.UpdatedDate,
        o.OrderDetails.Select(d => new OrderDetailDto(
            d.Id,
            d.ProductId,
            d.ProductName,
            d.UnitPrice,
            d.Quantity,
            d.Discount
        )).ToList()
    );
}
