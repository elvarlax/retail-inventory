namespace RetailInventory.Application.Seed;

public interface ISeedService
{
    Task<SeedResult> SeedAsync(int customers, int products, int orders);
}
