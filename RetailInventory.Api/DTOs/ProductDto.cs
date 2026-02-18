namespace RetailInventory.Api.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string SKU { get; set; } = default!;
    public int StockQuantity { get; set; }
    public decimal Price { get; set; }
}