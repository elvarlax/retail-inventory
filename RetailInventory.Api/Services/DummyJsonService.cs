using RetailInventory.Api.Models;

namespace RetailInventory.Api.Services;

public class DummyJsonService : IDummyJsonService
{
    private readonly HttpClient _httpClient;

    public DummyJsonService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DummyJsonProduct>> GetProductsAsync()
    {
        var allProducts = new List<DummyJsonProduct>();

        int limit = 50;
        int skip = 0;
        int total = int.MaxValue;

        while (skip < total)
        {
            var response = await _httpClient.GetFromJsonAsync<DummyJsonProductResponse>($"products?limit={limit}&skip={skip}");

            if (response == null || response.Products.Count == 0)
                break;

            allProducts.AddRange(response.Products);

            total = response.Total;
            skip += response.Products.Count;
        }

        return allProducts;
    }

    public async Task<List<DummyJsonUser>> GetUsersAsync()
    {
        var allUsers = new List<DummyJsonUser>();

        int limit = 50;
        int skip = 0;
        int total = int.MaxValue;

        while (skip < total)
        {
            var response = await _httpClient.GetFromJsonAsync<DummyJsonUserResponse>($"users?limit={limit}&skip={skip}");
            
            if (response == null || response.Users.Count == 0)
                break;

            allUsers.AddRange(response.Users);
            total = response.Total;
            skip += response.Users.Count;
        }

        return allUsers;
    }
}
