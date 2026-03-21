using MediatR;
using RetailInventory.Application.Common.DTOs;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.DTOs;
using RetailInventory.Domain;

namespace RetailInventory.Application.Orders.Queries;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, PagedResultDto<OrderDto>>
{
    private readonly IOrderQueryRepository _repository;

    public GetOrdersHandler(IOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<OrderDto>> Handle(GetOrdersQuery query, CancellationToken ct)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 || query.PageSize > 50 ? 10 : query.PageSize;

        OrderStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<OrderStatus>(query.Status, true, out var s))
                throw new BadRequestException("Invalid order status filter.");
            parsedStatus = s;
        }

        var skip = (pageNumber - 1) * pageSize;
        var total = await _repository.CountAsync(parsedStatus, query.CustomerId, query.Search);
        var items = await _repository.GetPagedAsync(skip, pageSize, parsedStatus, query.SortBy, query.SortDirection, query.CustomerId, query.Search);

        return new PagedResultDto<OrderDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
