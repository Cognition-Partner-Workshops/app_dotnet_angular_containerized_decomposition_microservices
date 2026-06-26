using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.API.Mapping;
using Order.API.ViewModels;
using Order.Infrastructure.Data;

namespace Order.API.Controllers;

// Resource controller mounted at the service ROOT. The API gateway strips the
// "/api/orders" prefix, so external "GET /api/orders" -> "GET /" here and
// "GET /api/orders/{id}" -> "GET /{id}". Absolute routes ("/...") are used so the
// service-internal paths match the gateway-stripped paths exactly.
[ApiController]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class OrderController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly ILogger<OrderController> _logger;

    public OrderController(OrderDbContext db, ILogger<OrderController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("/")]
    [ProducesResponseType(typeof(IEnumerable<OrderVM>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .ToListAsync();

        return Ok(orders.Select(o => o.ToViewModel()));
    }

    [HttpGet("/{id:int}")]
    [ProducesResponseType(typeof(OrderVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(id);

        return Ok(order.ToViewModel());
    }

    [HttpPost("/")]
    [ProducesResponseType(typeof(OrderVM), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] OrderVM model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = model.ToEntity();
        foreach (var line in model.OrderDetails)
            order.OrderDetails.Add(line.ToEntity());

        var now = DateTime.UtcNow;
        var userName = User.Identity?.Name;
        Stamp(order, now, userName, isNew: true);
        foreach (var detail in order.OrderDetails)
            Stamp(detail, now, userName, isNew: true);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = order.ToViewModel();
        return Created($"/{order.Id}", result);
    }

    [HttpPut("/{id:int}")]
    [ProducesResponseType(typeof(OrderVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] OrderVM model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(id);

        model.ApplyTo(order);

        var now = DateTime.UtcNow;
        var userName = User.Identity?.Name;

        _db.OrderDetails.RemoveRange(order.OrderDetails);
        order.OrderDetails.Clear();
        foreach (var line in model.OrderDetails)
        {
            var detail = line.ToEntity();
            Stamp(detail, now, userName, isNew: true);
            order.OrderDetails.Add(detail);
        }

        Stamp(order, now, userName, isNew: false);

        await _db.SaveChangesAsync();

        return Ok(order.ToViewModel());
    }

    [HttpDelete("/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(id);

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static void Stamp(Order.Domain.Entities.BaseEntity entity, DateTime now, string? userName, bool isNew)
    {
        if (isNew)
        {
            entity.CreatedDate = now;
            entity.CreatedBy = userName;
        }

        entity.UpdatedDate = now;
        entity.UpdatedBy = userName;
    }
}
