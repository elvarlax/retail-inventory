namespace RetailInventory.Application.Customers.Commands;

public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email
);
