namespace RetailInventory.Application.Orders.DTOs;

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string SKU { get; set; } = default!;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}
