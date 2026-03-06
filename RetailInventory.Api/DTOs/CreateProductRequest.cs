namespace RetailInventory.Api.DTOs;

public class CreateProductRequest
{
    public string Name { get; set; } = default!;
    public string SKU { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}
