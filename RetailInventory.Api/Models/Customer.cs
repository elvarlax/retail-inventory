namespace RetailInventory.Api.Models;

public class Customer
{
    public Guid Id { get; set; }
    public int ExternalId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
}