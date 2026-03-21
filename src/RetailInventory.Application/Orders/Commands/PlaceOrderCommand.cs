using MediatR;

namespace RetailInventory.Application.Orders.Commands;

public record PlaceOrderCommand(
    Guid CustomerId,
    List<OrderItemRequest> Items
) : IRequest<Guid>;

public record OrderItemRequest(Guid ProductId, int Quantity);
