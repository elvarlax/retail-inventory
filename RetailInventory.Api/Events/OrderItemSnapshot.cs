namespace RetailInventory.Api.Events;

public class OrderItemSnapshot
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}