namespace RetailInventory.Application.Orders.Commands;

public record CancelOrderCommand(Guid OrderId, Guid? RequestingCustomerId = null);
