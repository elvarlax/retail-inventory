namespace RetailInventory.Application.Orders.Queries;

public record GetOrdersQuery(
    int PageNumber,
    int PageSize,
    string? Status,
    string? SortBy,
    string? SortDirection,
    Guid? CustomerId,
    string? Search = null);
