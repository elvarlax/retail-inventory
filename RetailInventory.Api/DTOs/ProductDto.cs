using System.Text.Json.Serialization;

namespace RetailInventory.Api.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    [JsonPropertyName("sku")]
    public string SKU { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public decimal Price { get; set; }
}
