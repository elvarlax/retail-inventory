namespace RetailInventory.Application.Orders.Events;

public class OrderStatusChangedV1
{
    public Guid EventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid OrderId { get; set; }
    public string PreviousStatus { get; set; } = default!;
    public string NewStatus { get; set; } = default!;
}
