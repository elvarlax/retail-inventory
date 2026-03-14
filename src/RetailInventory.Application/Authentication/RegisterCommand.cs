namespace RetailInventory.Application.Authentication;

public record RegisterCommand(string FirstName, string LastName, string Email, string Password);
