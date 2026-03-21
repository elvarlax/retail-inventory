using MediatR;
using RetailInventory.Application.Common.DTOs;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Products.DTOs;

namespace RetailInventory.Application.Products.Queries;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, PagedResultDto<ProductDto>>
{
    private readonly IProductQueryRepository _queryRepository;

    public GetProductsHandler(IProductQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<PagedResultDto<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 || query.PageSize > 50 ? 10 : query.PageSize;
        var skip = (pageNumber - 1) * pageSize;

        var totalCount = await _queryRepository.CountAsync(query.Search);
        var items = await _queryRepository.GetPagedAsync(skip, pageSize, query.SortBy, query.SortDirection, query.Search);

        return new PagedResultDto<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
