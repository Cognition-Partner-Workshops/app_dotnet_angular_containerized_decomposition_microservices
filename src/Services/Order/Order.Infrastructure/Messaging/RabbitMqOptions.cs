namespace Order.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";
    public string Exchange { get; set; } = "quickapp.events";
    public string OrderPlacedRoutingKey { get; set; } = "order.placed";

    /// <summary>
    /// Queue declared and bound by the publisher so the event survives until a consumer
    /// binds to it. The Notification service is the intended consumer.
    /// </summary>
    public string OrderPlacedQueue { get; set; } = "notification.order-placed";
}
