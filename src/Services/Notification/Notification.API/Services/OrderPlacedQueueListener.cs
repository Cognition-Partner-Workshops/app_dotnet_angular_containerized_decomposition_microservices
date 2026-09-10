using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;

namespace Notification.API.Services;

/// <summary>
/// Subscribes to the durable OrderPlaced queue and dispatches each message into
/// <see cref="OrderEventConsumer"/>, the same handler the HTTP ingestion endpoint uses.
/// </summary>
public sealed class OrderPlacedQueueListener : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderPlacedQueueListener> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public OrderPlacedQueueListener(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<OrderPlacedQueueListener> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration["RabbitMq:ConnectionString"] ?? "amqp://guest:guest@rabbitmq:5672";
        var exchange = _configuration["RabbitMq:Exchange"] ?? "quickapp.events";
        var routingKey = _configuration["RabbitMq:OrderPlacedRoutingKey"] ?? "order.placed";
        var queue = _configuration["RabbitMq:OrderPlacedQueue"] ?? "notification.order-placed";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(connectionString),
                    ClientProvidedName = "notification-service"
                };

                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false,
                    cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                    cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(queue, exchange, routingKey, cancellationToken: stoppingToken);
                await _channel.BasicQosAsync(0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += (_, args) => HandleAsync(args, stoppingToken);

                await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, cancellationToken: stoppingToken);

                _logger.LogInformation("Listening for OrderPlacedEvent on {Queue}", queue);

                while (!stoppingToken.IsCancellationRequested && _connection.IsOpen)
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Broker unavailable; retrying in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        try
        {
            var orderEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(args.Body.Span);
            if (orderEvent is null)
            {
                _logger.LogWarning("Discarding unreadable OrderPlacedEvent message {DeliveryTag}", args.DeliveryTag);
                await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<OrderEventConsumer>();
            await handler.HandleOrderPlaced(orderEvent);

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process OrderPlacedEvent message {DeliveryTag}", args.DeliveryTag);
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
