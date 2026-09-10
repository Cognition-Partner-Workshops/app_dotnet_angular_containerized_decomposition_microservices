using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Order.Domain.Interfaces;
using RabbitMQ.Client;
using Shared.Contracts.Events;

namespace Order.Infrastructure.Messaging;

public sealed class RabbitMqOrderEventPublisher : IOrderEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqOrderEventPublisher> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqOrderEventPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqOrderEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishOrderPlacedAsync(OrderPlacedEvent orderPlaced, CancellationToken cancellationToken = default)
    {
        var channel = await GetChannelAsync(cancellationToken);
        var body = JsonSerializer.SerializeToUtf8Bytes(orderPlaced);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Type = nameof(OrderPlacedEvent),
            MessageId = orderPlaced.OrderId.ToString()
        };

        await channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: _options.OrderPlacedRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Published OrderPlacedEvent for order {OrderId} to {Exchange}/{RoutingKey} ({Bytes} bytes)",
            orderPlaced.OrderId, _options.Exchange, _options.OrderPlacedRoutingKey, body.Length);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            if (_connection is not { IsOpen: true })
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_options.ConnectionString),
                    ClientProvidedName = "order-service"
                };
                _connection = await factory.CreateConnectionAsync(cancellationToken);
            }

            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false,
                cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync(_options.OrderPlacedQueue, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(_options.OrderPlacedQueue, _options.Exchange, _options.OrderPlacedRoutingKey,
                cancellationToken: cancellationToken);

            return _channel;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }
}
