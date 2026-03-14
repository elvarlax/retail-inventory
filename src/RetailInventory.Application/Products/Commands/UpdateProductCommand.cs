namespace RetailInventory.Application.Products.Commands;

public record UpdateProductCommand(Guid Id, string Name, string SKU, string? ImageUrl, decimal Price, int StockQuantity);
