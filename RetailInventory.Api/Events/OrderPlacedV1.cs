namespace RetailInventory.Api.Events;

public class OrderPlacedV1
{
    public Guid EventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemSnapshot> Items { get; set; } = [];
}