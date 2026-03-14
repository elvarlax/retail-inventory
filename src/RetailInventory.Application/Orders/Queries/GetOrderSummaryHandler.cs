using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.DTOs;

namespace RetailInventory.Application.Orders.Queries;

public class GetOrderSummaryHandler
{
    private readonly IOrderQueryRepository _repository;

    public GetOrderSummaryHandler(IOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderSummaryDto> Handle(GetOrderSummaryQuery query)
    {
        return await _repository.GetSummaryAsync();
    }
}
