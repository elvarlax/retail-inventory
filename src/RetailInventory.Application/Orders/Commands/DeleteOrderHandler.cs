using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Orders.Commands;

public class DeleteOrderHandler
{
    private readonly IOrderRepository _repository;

    public DeleteOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteOrderCommand command)
    {
        var order = await _repository.GetByIdAsync(command.OrderId)
            ?? throw new NotFoundException("Order not found.");

        await _repository.DeleteAsync(order);
        await _repository.SaveChangesAsync();
    }
}
