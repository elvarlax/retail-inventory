using RetailInventory.Application.Common.DTOs;
using RetailInventory.Application.Customers.DTOs;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Customers.Queries;

public class GetCustomersHandler
{
    private readonly ICustomerQueryRepository _queryRepository;

    public GetCustomersHandler(ICustomerQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<PagedResultDto<CustomerDto>> Handle(GetCustomersQuery query)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 || query.PageSize > 50 ? 10 : query.PageSize;
        var skip = (pageNumber - 1) * pageSize;

        var totalCount = await _queryRepository.CountAsync(query.Search);
        var items = await _queryRepository.GetPagedAsync(skip, pageSize, query.SortBy, query.SortDirection, query.Search);

        return new PagedResultDto<CustomerDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
