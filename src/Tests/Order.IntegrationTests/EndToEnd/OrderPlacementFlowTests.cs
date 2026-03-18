using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Order.IntegrationTests.Infrastructure;
using Shared.Contracts.DTOs;

namespace Order.IntegrationTests.EndToEnd;

/// <summary>
/// End-to-end flow tests that simulate the sequence of HTTP calls the
/// QuickApp monolith would make when a customer places an order:
///
///   1. A customer record exists (represented here by a CustomerId integer,
///      since the Customer microservice is a separate bounded context).
///   2. The monolith POSTs to the Order microservice to create an order.
///   3. The monolith GETs the order back to confirm it was persisted correctly.
///
/// These tests exercise the full HTTP pipeline of the Order service using an
/// in-memory database, validating that the extracted microservice honours the
/// same contract the monolith relied on.
/// </summary>
public class OrderPlacementFlowTests : IClassFixture<OrderServiceFactory>
{
    private readonly HttpClient _client;

    public OrderPlacementFlowTests(OrderServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Core happy-path: create a customer (by ID), place an order, verify it
    /// appears in the Order service.
    /// </summary>
    [Fact]
    public async Task PlaceOrder_ThenVerifyInOrderService_FullRoundTrip()
    {
        // Arrange – the monolith would have already created the customer;
        // here we represent the customer by their integer ID.
        const int customerId = 100;

        var createOrderDto = new CreateOrderDto(
            Discount: 5.0m,
            Comments: "E2E flow test order",
            CashierId: "cashier-e2e",
            CustomerId: customerId,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 29.99m, Quantity: 2, Discount: 0m, ProductId: 10),
                new(UnitPrice: 14.99m, Quantity: 1, Discount: 0.5m, ProductId: 11)
            });

        // Act – place the order
        var createResponse = await _client.PostAsJsonAsync("/api/order", createOrderDto);

        // Assert – creation succeeded
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        createdOrder.Should().NotBeNull();
        createdOrder!.Id.Should().BeGreaterThan(0);

        // Act – verify the order is retrievable (as the monolith would do)
        var getResponse = await _client.GetAsync($"/api/order/{createdOrder.Id}");

        // Assert – order is persisted with correct data
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedOrder = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        fetchedOrder.Should().NotBeNull();
        fetchedOrder!.Id.Should().Be(createdOrder.Id);
        fetchedOrder.CustomerId.Should().Be(customerId);
        fetchedOrder.Comments.Should().Be("E2E flow test order");
        fetchedOrder.CashierId.Should().Be("cashier-e2e");
        fetchedOrder.Discount.Should().Be(5.0m);
        fetchedOrder.OrderDetails.Should().HaveCount(2);
        fetchedOrder.CreatedDate.Should().NotBe(default(DateTime));
    }

    /// <summary>
    /// Simulates the monolith listing all orders for a customer after placing one.
    /// </summary>
    [Fact]
    public async Task PlaceOrder_ThenListAllOrders_OrderAppearsInList()
    {
        const int customerId = 101;

        var createDto = OrderTestFixture.BuildCreateOrderDto(
            customerId: customerId,
            comments: "List verification order");

        var createResponse = await _client.PostAsJsonAsync("/api/order", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        created.Should().NotBeNull();

        var listResponse = await _client.GetAsync("/api/order");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await listResponse.Content.ReadFromJsonAsync<List<OrderDto>>();
        orders.Should().NotBeNull();
        orders!.Should().Contain(o => o.Id == created!.Id && o.CustomerId == customerId);
    }

    /// <summary>
    /// Simulates the monolith updating an order after it was placed
    /// (e.g. a cashier is assigned post-creation).
    /// </summary>
    [Fact]
    public async Task PlaceOrder_ThenUpdateCashier_ChangesReflectedOnGet()
    {
        const int customerId = 102;

        var created = await OrderTestFixture.CreateOrderAsync(_client,
            OrderTestFixture.BuildCreateOrderDto(customerId: customerId, cashierId: null));

        // Monolith assigns a cashier after the fact
        var updateDto = new UpdateOrderDto(
            Discount: created.Discount,
            Comments: created.Comments,
            CashierId: "assigned-cashier",
            CustomerId: customerId);

        var updateResponse = await _client.PutAsJsonAsync($"/api/order/{created.Id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        updated.Should().NotBeNull();
        updated!.CashierId.Should().Be("assigned-cashier");
    }

    /// <summary>
    /// Simulates the monolith cancelling (deleting) an order and confirming
    /// it is no longer accessible.
    /// </summary>
    [Fact]
    public async Task PlaceOrder_ThenCancelOrder_OrderNotFoundAfterDeletion()
    {
        const int customerId = 103;

        var created = await OrderTestFixture.CreateOrderAsync(_client,
            OrderTestFixture.BuildCreateOrderDto(customerId: customerId, comments: "To be cancelled"));

        // Monolith cancels the order
        var deleteResponse = await _client.DeleteAsync($"/api/order/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Monolith tries to retrieve the cancelled order
        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Simulates multiple customers placing orders concurrently and verifies
    /// each order is independently retrievable.
    /// </summary>
    [Fact]
    public async Task MultipleCustomers_PlaceOrders_EachOrderIsIndependent()
    {
        var customerIds = new[] { 200, 201, 202 };

        var createTasks = customerIds.Select(cid =>
            OrderTestFixture.CreateOrderAsync(_client,
                OrderTestFixture.BuildCreateOrderDto(customerId: cid, comments: $"Order for customer {cid}")));

        var createdOrders = await Task.WhenAll(createTasks);

        foreach (var order in createdOrders)
        {
            var getResponse = await _client.GetAsync($"/api/order/{order.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var fetched = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
            fetched.Should().NotBeNull();
            fetched!.Id.Should().Be(order.Id);
            fetched.Comments.Should().Be($"Order for customer {order.CustomerId}");
        }
    }

    /// <summary>
    /// Verifies that the Location header returned on creation points to the
    /// correct resource, matching the monolith's expectation of a stable URL.
    /// </summary>
    [Fact]
    public async Task PlaceOrder_LocationHeader_PointsToCreatedResource()
    {
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 300);

        var createResponse = await _client.PostAsJsonAsync("/api/order", dto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location;
        location.Should().NotBeNull();

        // Follow the Location header to confirm the resource exists
        var getResponse = await _client.GetAsync(location);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.CustomerId.Should().Be(300);
    }

    /// <summary>
    /// Verifies that an order with many line items is persisted and retrieved
    /// completely, matching the monolith's bulk-order use case.
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithManyLineItems_AllDetailsPersistedAndRetrieved()
    {
        const int lineItemCount = 5;
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 301, detailCount: lineItemCount);

        var created = await OrderTestFixture.CreateOrderAsync(_client, dto);

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        fetched.Should().NotBeNull();
        fetched!.OrderDetails.Should().HaveCount(lineItemCount);

        // Each detail must reference the parent order
        fetched.OrderDetails.Should().OnlyContain(d => d.OrderId == fetched.Id);
    }
}
