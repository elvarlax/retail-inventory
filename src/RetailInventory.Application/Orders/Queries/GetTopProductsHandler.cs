using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.DTOs;

namespace RetailInventory.Application.Orders.Queries;

public class GetTopProductsHandler
{
    private readonly IOrderQueryRepository _repository;

    public GetTopProductsHandler(IOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public Task<List<TopProductDto>> Handle(GetTopProductsQuery query) =>
        _repository.GetTopProductsAsync(query.Limit);
}
