using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.API.DTOs;
using Order.Domain.Entities;
using Order.Infrastructure.Data;

namespace Order.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly OrderDbContext _context;

    public OrderController(OrderDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .ToListAsync();

        var result = orders.Select(MapToDto).ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound();

        return Ok(MapToDto(order));
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var order = new Order.Domain.Entities.Order
        {
            Discount = dto.Discount,
            Comments = dto.Comments,
            CashierId = dto.CashierId,
            CustomerId = dto.CustomerId,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        foreach (var detailDto in dto.OrderDetails)
        {
            order.OrderDetails.Add(new OrderDetail
            {
                UnitPrice = detailDto.UnitPrice,
                Quantity = detailDto.Quantity,
                Discount = detailDto.Discount,
                ProductId = detailDto.ProductId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToDto(order));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound();

        order.Discount = dto.Discount;
        order.Comments = dto.Comments;
        order.CashierId = dto.CashierId;
        order.CustomerId = dto.CustomerId;
        order.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapToDto(order));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order is null)
            return NotFound();

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static OrderDto MapToDto(Order.Domain.Entities.Order order) => new()
    {
        Id = order.Id,
        Discount = order.Discount,
        Comments = order.Comments,
        CashierId = order.CashierId,
        CustomerId = order.CustomerId,
        OrderDetails = order.OrderDetails.Select(od => new OrderDetailDto
        {
            Id = od.Id,
            UnitPrice = od.UnitPrice,
            Quantity = od.Quantity,
            Discount = od.Discount,
            ProductId = od.ProductId
        }).ToList()
    };
}
