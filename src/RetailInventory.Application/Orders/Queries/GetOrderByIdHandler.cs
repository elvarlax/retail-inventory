using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.DTOs;

namespace RetailInventory.Application.Orders.Queries;

public class GetOrderByIdHandler
{
    private readonly IOrderQueryRepository _repository;

    public GetOrderByIdHandler(IOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery query)
    {
        return await _repository.GetByIdAsync(query.Id)
            ?? throw new NotFoundException("Order not found.");
    }
}
