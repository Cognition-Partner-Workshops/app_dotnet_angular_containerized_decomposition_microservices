using Microsoft.AspNetCore.Mvc;
using Order.Domain.Entities;
using Order.Domain.Interfaces;
using Shared.Contracts.Events;

namespace Order.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderRepository _repository;
    private readonly HttpClient _productClient;
    private readonly HttpClient _notificationClient;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrderRepository repository,
        IHttpClientFactory httpClientFactory,
        ILogger<OrderController> logger)
    {
        _repository = repository;
        _productClient = httpClientFactory.CreateClient("ProductService");
        _notificationClient = httpClientFactory.CreateClient("NotificationService");
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _repository.GetAllAsync();
        return Ok(orders.Select(o => new
        {
            o.Id,
            o.CustomerId,
            o.ProductId,
            o.ProductName,
            o.Quantity,
            o.UnitPrice,
            o.TotalAmount,
            o.Status,
            o.CreatedAt
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order is null)
            return NotFound();

        return Ok(new
        {
            order.Id,
            order.CustomerId,
            order.ProductId,
            order.ProductName,
            order.Quantity,
            order.UnitPrice,
            order.TotalAmount,
            order.Status,
            order.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        if (request.CustomerId == Guid.Empty)
            return BadRequest(new { error = "A valid customer ID is required." });

        if (request.ProductId == Guid.Empty)
            return BadRequest(new { error = "A valid product ID is required." });

        if (request.Quantity <= 0)
            return BadRequest(new { error = "Quantity must be greater than zero." });

        // Validate product exists by calling Product service
        ProductDto? product;
        try
        {
            var response = await _productClient.GetAsync($"/api/product/{request.ProductId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Product {ProductId} not found", request.ProductId);
                return BadRequest(new { error = $"Product {request.ProductId} does not exist." });
            }
            product = await response.Content.ReadFromJsonAsync<ProductDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Product service");
            return StatusCode(503, new { error = "Product service is unavailable." });
        }

        if (product is null)
            return BadRequest(new { error = $"Product {request.ProductId} does not exist." });

        var totalAmount = product.Price * request.Quantity;

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            ProductName = product.Name,
            Quantity = request.Quantity,
            UnitPrice = product.Price,
            TotalAmount = totalAmount,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(order);
        _logger.LogInformation("Order {OrderId} created for customer {CustomerId}", order.Id, order.CustomerId);

        // Publish OrderPlacedEvent to Notification service
        try
        {
            var orderEvent = new OrderPlacedEvent(
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                order.CreatedAt);

            await _notificationClient.PostAsJsonAsync("/api/notification/events/order-placed", orderEvent);
            _logger.LogInformation("OrderPlacedEvent published for order {OrderId}", order.Id);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to publish OrderPlacedEvent for order {OrderId}", order.Id);
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new
        {
            order.Id,
            order.CustomerId,
            order.ProductId,
            order.ProductName,
            order.Quantity,
            order.UnitPrice,
            order.TotalAmount,
            order.Status,
            order.CreatedAt
        });
    }
}

public record CreateOrderRequest(
    Guid CustomerId,
    Guid ProductId,
    int Quantity);

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity);
