using Microsoft.AspNetCore.Mvc;
using Order.Domain.Interfaces;
using Shared.Contracts.DTOs;

namespace Order.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderRepository orderRepository, ILogger<OrderController> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
    {
        var orders = await _orderRepository.GetAllAsync();
        var dtos = orders.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        return Ok(MapToDto(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
    {
        var order = new Domain.Entities.Order
        {
            Discount = dto.Discount,
            Comments = dto.Comments,
            CashierId = dto.CashierId,
            CustomerId = dto.CustomerId,
            OrderDetails = dto.OrderDetails.Select(d => new Domain.Entities.OrderDetail
            {
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                Discount = d.Discount,
                ProductId = d.ProductId
            }).ToList()
        };

        var created = await _orderRepository.CreateAsync(order);
        _logger.LogInformation("Order {OrderId} created for customer {CustomerId}", created.Id, created.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        order.Discount = dto.Discount;
        order.Comments = dto.Comments;
        order.CashierId = dto.CashierId;
        order.CustomerId = dto.CustomerId;

        await _orderRepository.UpdateAsync(order);
        _logger.LogInformation("Order {OrderId} updated", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        await _orderRepository.DeleteAsync(id);
        _logger.LogInformation("Order {OrderId} deleted", id);

        return NoContent();
    }

    private static OrderDto MapToDto(Domain.Entities.Order order)
    {
        return new OrderDto(
            order.Id,
            order.Discount,
            order.Comments,
            order.CashierId,
            order.CustomerId,
            order.CreatedDate,
            order.UpdatedDate,
            order.OrderDetails.Select(d => new OrderDetailDto(
                d.Id,
                d.UnitPrice,
                d.Quantity,
                d.Discount,
                d.ProductId,
                d.OrderId
            )).ToList()
        );
    }
}
