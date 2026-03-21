using MediatR;

namespace RetailInventory.Application.Products.Commands;

public record CreateProductCommand(
    string Name,
    string SKU,
    string? ImageUrl,
    decimal Price,
    int StockQuantity
) : IRequest<Guid>;
