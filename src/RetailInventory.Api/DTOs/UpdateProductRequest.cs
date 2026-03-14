namespace RetailInventory.Api.DTOs;

public record UpdateProductRequest(string Name, string SKU, string? ImageUrl, decimal Price, int StockQuantity);
