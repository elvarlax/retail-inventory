using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Services;

public interface ISeedService
{
    Task<SeedResultResponse> SeedAsync(int customers, int products, int orders);
}
