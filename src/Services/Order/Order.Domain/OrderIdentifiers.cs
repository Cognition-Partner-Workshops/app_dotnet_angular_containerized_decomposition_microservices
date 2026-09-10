namespace Order.Domain;

/// <summary>
/// The order context keys orders and customers by int (as the monolith did), while
/// <c>Shared.Contracts.Events.OrderPlacedEvent</c> carries Guids. These helpers derive a
/// stable Guid from an int id so a published event can be traced back to its row.
/// </summary>
public static class OrderIdentifiers
{
    private static readonly byte[] OrderTag = [0x71, 0x75, 0x69, 0x63, 0x6B, 0x6F, 0x72, 0x64, 0x65, 0x72, 0x00, 0x01];
    private static readonly byte[] CustomerTag = [0x71, 0x75, 0x69, 0x63, 0x6B, 0x63, 0x75, 0x73, 0x74, 0x6F, 0x00, 0x02];

    public static Guid ToOrderGuid(int orderId) => Compose(orderId, OrderTag);

    public static Guid ToCustomerGuid(int customerId) => Compose(customerId, CustomerTag);

    private static Guid Compose(int id, byte[] tag)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, id);
        tag.CopyTo(bytes[4..]);
        return new Guid(bytes);
    }
}
