namespace RetailInventory.Api.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Customer Customer { get; set; } = default!;
    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}