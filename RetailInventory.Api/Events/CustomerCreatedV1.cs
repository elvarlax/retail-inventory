namespace RetailInventory.Api.Events;

public class CustomerCreatedV1
{
    public Guid EventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid CustomerId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
}