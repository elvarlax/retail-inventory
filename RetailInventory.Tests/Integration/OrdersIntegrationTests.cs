using FluentAssertions;
using RetailInventory.Tests.Helpers;
using System.Net;

namespace RetailInventory.Tests;

public class OrdersIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOrders_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}