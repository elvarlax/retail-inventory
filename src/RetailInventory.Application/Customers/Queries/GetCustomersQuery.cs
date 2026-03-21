using MediatR;
using RetailInventory.Application.Common.DTOs;
using RetailInventory.Application.Customers.DTOs;

namespace RetailInventory.Application.Customers.Queries;

public record GetCustomersQuery(
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    string? Search = null
) : IRequest<PagedResultDto<CustomerDto>>;
