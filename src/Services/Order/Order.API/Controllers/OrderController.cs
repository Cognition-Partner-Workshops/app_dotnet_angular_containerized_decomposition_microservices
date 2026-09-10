using Microsoft.AspNetCore.Mvc;
using Order.API.Services;
using Order.API.ViewModels;
using Order.Domain;
using Order.Domain.Interfaces;
using Order.Infrastructure.Orders;

namespace Order.API.Controllers;

/// <summary>
/// Order endpoints. The gateway strips the <c>/api/orders</c> prefix before forwarding, so the
/// routes are served at the service root as well as under <c>/api/orders</c> for direct calls.
/// </summary>
[ApiController]
[Route("/")]
[Route("api/orders")]
[Produces("application/json")]
public class OrderController : ControllerBase
{
    private readonly IOrdersService _orders;
    private readonly OrderPlacementService _placement;
    private readonly OrderViewModelMapper _mapper;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrdersService orders,
        OrderPlacementService placement,
        OrderViewModelMapper mapper,
        ILogger<OrderController> logger)
    {
        _orders = orders;
        _placement = placement;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<OrderVM>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var orders = await _orders.GetAllOrdersAsync(page, pageSize, cancellationToken);
        return Ok(await _mapper.ToEnrichedViewModelsAsync(orders, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderVM>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetOrderByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        return Ok(await _mapper.ToEnrichedViewModelAsync(order, cancellationToken));
    }

    [HttpGet("customer/{customerId:int}")]
    [ProducesResponseType<IEnumerable<OrderVM>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer(int customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _orders.GetOrdersByCustomerAsync(customerId, cancellationToken);
        return Ok(await _mapper.ToEnrichedViewModelsAsync(orders, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<OrderVM>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Place([FromBody] OrderSaveVM model, CancellationToken cancellationToken = default)
    {
        var placed = await _placement.PlaceOrderAsync(OrderViewModelMapper.ToEntity(model), cancellationToken);

        _logger.LogInformation(
            "Placed order {OrderId} for customer {CustomerId} (event order id {EventOrderId})",
            placed.Id, placed.CustomerId, OrderIdentifiers.ToOrderGuid(placed.Id));

        return CreatedAtAction(nameof(GetById), new { id = placed.Id }, await _mapper.ToEnrichedViewModelAsync(placed, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<OrderVM>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] OrderSaveVM model, CancellationToken cancellationToken = default)
    {
        var updated = await _orders.UpdateOrderAsync(OrderViewModelMapper.ToEntity(model, id), cancellationToken);
        if (updated is null)
            return NotFound();

        return Ok(await _mapper.ToEnrichedViewModelAsync(updated, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        return await _orders.DeleteOrderAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
