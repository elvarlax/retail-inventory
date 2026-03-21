using MediatR;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.DTOs;

namespace RetailInventory.Application.Orders.Queries;

public class GetOrderSummaryHandler : IRequestHandler<GetOrderSummaryQuery, OrderSummaryDto>
{
    private readonly IOrderQueryRepository _repository;

    public GetOrderSummaryHandler(IOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderSummaryDto> Handle(GetOrderSummaryQuery query, CancellationToken ct)
    {
        return await _repository.GetSummaryAsync();
    }
}
