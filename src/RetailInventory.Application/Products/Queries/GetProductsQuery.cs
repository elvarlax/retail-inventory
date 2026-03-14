namespace RetailInventory.Application.Products.Queries;

public record GetProductsQuery(
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    string? Search = null
);
