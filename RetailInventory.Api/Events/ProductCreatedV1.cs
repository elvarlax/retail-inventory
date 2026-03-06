namespace RetailInventory.Api.Events;

public class ProductCreatedV1
{
    public Guid EventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = default!;
    public string SKU { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}