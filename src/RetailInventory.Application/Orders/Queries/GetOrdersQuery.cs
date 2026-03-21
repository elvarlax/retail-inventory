using MediatR;
using RetailInventory.Application.Common.DTOs;
using RetailInventory.Application.Orders.DTOs;

namespace RetailInventory.Application.Orders.Queries;

public record GetOrdersQuery(
    int PageNumber,
    int PageSize,
    string? Status,
    string? SortBy,
    string? SortDirection,
    Guid? CustomerId,
    string? Search = null) : IRequest<PagedResultDto<OrderDto>>;
