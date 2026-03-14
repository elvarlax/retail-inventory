namespace RetailInventory.Application.Products.Commands;

public record RestockProductCommand(Guid Id, int Quantity);
