using MediatR;
using RetailInventory.Application.Common.DTOs;
using RetailInventory.Application.Products.DTOs;

namespace RetailInventory.Application.Products.Queries;

public record GetProductsQuery(
    int PageNumber,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    string? Search = null
) : IRequest<PagedResultDto<ProductDto>>;
