using RetailInventory.Api.Models;

namespace RetailInventory.Api.Services;

public interface IDummyJsonService
{
    Task<List<DummyJsonProduct>> GetProductsAsync();
    Task<List<DummyJsonUser>> GetUsersAsync();
}