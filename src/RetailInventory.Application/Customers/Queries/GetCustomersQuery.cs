namespace RetailInventory.Application.Customers.Queries;

public record GetCustomersQuery(
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    string? Search = null
);
