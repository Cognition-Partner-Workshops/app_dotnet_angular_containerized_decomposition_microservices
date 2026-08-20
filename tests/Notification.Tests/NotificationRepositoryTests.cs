using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;
using Notification.Infrastructure.Repositories;

namespace Notification.Tests;

public class NotificationRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsNotification()
    {
        await using var database = await TestDatabase.CreateAsync();
        var notification = CreateNotification();

        var result = await database.Repository.AddAsync(notification);

        result.Should().BeSameAs(notification);
        var stored = await database.Context.OrderNotifications
            .AsNoTracking()
            .SingleAsync();
        stored.Id.Should().Be(notification.Id);
        stored.OrderTotal.Should().Be(notification.OrderTotal);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotificationWhenFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        var notification = await database.Repository.AddAsync(CreateNotification());

        var result = await database.Repository.GetByIdAsync(notification.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await database.Repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrderIdAsync_ReturnsNotificationsInDescendingCreatedAtOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        var orderId = Guid.NewGuid();
        var older = CreateNotification(orderId, new DateTime(2025, 1, 1));
        var newer = CreateNotification(orderId, new DateTime(2025, 1, 2));
        await database.Repository.AddAsync(older);
        await database.Repository.AddAsync(newer);

        var result = await database.Repository.GetByOrderIdAsync(orderId);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(newer.Id);
        result[1].Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRequestedPage()
    {
        await using var database = await TestDatabase.CreateAsync();
        for (var index = 0; index < 5; index++)
        {
            await database.Repository.AddAsync(
                CreateNotification(Guid.NewGuid(), new DateTime(2025, 1, 1).AddDays(index)));
        }

        var result = await database.Repository.GetAllAsync(page: 2, pageSize: 2);

        result.Should().HaveCount(2);
        result[0].CreatedAt.Should().Be(new DateTime(2025, 1, 3));
        result[1].CreatedAt.Should().Be(new DateTime(2025, 1, 2));
    }

    private static OrderNotification CreateNotification(
        Guid? orderId = null,
        DateTime? createdAt = null)
    {
        return new OrderNotification
        {
            Id = Guid.NewGuid(),
            OrderId = orderId ?? Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderTotal = 12345m,
            CustomerEmail = "customer@example.com",
            CustomerName = "Valued Customer",
            Type = NotificationType.OrderConfirmation,
            Status = NotificationStatus.Rendered,
            RenderedSubject = "Order Confirmed",
            RenderedBody = "<p>Rendered</p>",
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(
            SqliteConnection connection,
            NotificationDbContext context,
            NotificationRepository repository)
        {
            _connection = connection;
            Context = context;
            Repository = repository;
        }

        public NotificationDbContext Context { get; }
        public NotificationRepository Repository { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new NotificationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase(
                connection,
                context,
                new NotificationRepository(context));
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
