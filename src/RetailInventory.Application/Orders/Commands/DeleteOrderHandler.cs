using MediatR;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Orders.Commands;

public class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand>
{
    private readonly IOrderRepository _repository;

    public DeleteOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteOrderCommand command, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, ct)
            ?? throw new NotFoundException("Order not found.");

        await _repository.DeleteAsync(order, ct);
        await _repository.SaveChangesAsync(ct);
    }
}
