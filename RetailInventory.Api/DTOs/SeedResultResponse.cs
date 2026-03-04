namespace RetailInventory.Api.DTOs;

public class SeedResultResponse
{
    public int Customers { get; set; }
    public int Products { get; set; }
    public int Orders { get; set; }
    public int EventsEmitted { get; set; }
}
