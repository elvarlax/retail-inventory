namespace RetailInventory.Application.Customers.Commands;

public record UpdateCustomerCommand(Guid Id, string FirstName, string LastName, string Email);
