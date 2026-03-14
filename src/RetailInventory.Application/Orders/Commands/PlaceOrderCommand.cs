namespace RetailInventory.Application.Orders.Commands;

public record PlaceOrderCommand(
    Guid CustomerId,
    List<OrderItemRequest> Items
);

public record OrderItemRequest(Guid ProductId, int Quantity);
