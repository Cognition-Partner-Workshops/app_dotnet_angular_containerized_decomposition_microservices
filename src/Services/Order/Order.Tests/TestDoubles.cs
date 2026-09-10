using Microsoft.EntityFrameworkCore;
using Order.Domain.Interfaces;
using Order.Domain.Models;
using Order.Infrastructure.Data;
using Shared.Contracts.Events;

namespace Order.Tests;

internal static class TestDb
{
    public static OrderDbContext NewContext() =>
        new(new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase($"orders-{Guid.NewGuid()}")
            .Options);
}

internal sealed class RecordingEventPublisher : IOrderEventPublisher
{
    public List<OrderPlacedEvent> Published { get; } = [];

    public Task PublishOrderPlacedAsync(OrderPlacedEvent orderPlaced, CancellationToken cancellationToken = default)
    {
        Published.Add(orderPlaced);
        return Task.CompletedTask;
    }
}

internal sealed class ThrowingEventPublisher : IOrderEventPublisher
{
    public Task PublishOrderPlacedAsync(OrderPlacedEvent orderPlaced, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("broker unavailable");
}

internal sealed class StubEnrichmentClient : IOrderEnrichmentClient
{
    public Dictionary<int, RelatedParty> Customers { get; } = [];
    public Dictionary<int, RelatedParty> Products { get; } = [];
    public Dictionary<string, RelatedParty> Cashiers { get; } = [];

    public Task<RelatedParty?> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Customers.GetValueOrDefault(customerId));

    public Task<RelatedParty?> GetProductAsync(int productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.GetValueOrDefault(productId));

    public Task<RelatedParty?> GetCashierAsync(string cashierId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Cashiers.GetValueOrDefault(cashierId));
}

internal sealed class UnavailableEnrichmentClient : IOrderEnrichmentClient
{
    public Task<RelatedParty?> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<RelatedParty?>(null);

    public Task<RelatedParty?> GetProductAsync(int productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<RelatedParty?>(null);

    public Task<RelatedParty?> GetCashierAsync(string cashierId, CancellationToken cancellationToken = default) =>
        Task.FromResult<RelatedParty?>(null);
}
