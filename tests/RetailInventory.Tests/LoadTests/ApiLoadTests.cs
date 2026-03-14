using NBomber.CSharp;
using NBomber.Contracts;
using NBomber.Http.CSharp;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RetailInventory.Tests.LoadTests;

// Load tests against a running API instance.
// Requires the Docker stack to be up: docker compose up -d
//
// Run separately from unit/integration tests:
//   dotnet test --filter "Category=LoadTest"
public class ApiLoadTests
{
    private const string BaseUrl = "http://localhost:8080";
    private const int RequestRatePerSecond = 5;
    private static readonly TimeSpan TestDuration = TimeSpan.FromSeconds(30);

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var body = JsonSerializer.Serialize(new { email = "admin@local", password = "Admin123!" });
        var response = await client.PostAsync("/auth/login",
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static IResponse CreateResponse(string statusCode)
    {
        var isSuccess =
            statusCode.StartsWith("2", StringComparison.Ordinal) ||
            Enum.TryParse<HttpStatusCode>(statusCode, ignoreCase: true, out var parsedStatusCode) &&
            (int)parsedStatusCode is >= 200 and < 300;

        return isSuccess
            ? Response.Ok(statusCode: statusCode)
            : Response.Fail(statusCode: statusCode);
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task SmokeTest_KeyEndpoints_ShouldHandleLight_Load()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await GetTokenAsync(httpClient);

        var products = Scenario.Create("get_products", async _ =>
        {
            var request = Http.CreateRequest("GET", $"{BaseUrl}/api/v1/products?pageNumber=1&pageSize=10")
                .WithHeader("Authorization", $"Bearer {token}");

            var response = await Http.Send(httpClient, request);
            return CreateResponse(response.StatusCode);
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: RequestRatePerSecond, interval: TimeSpan.FromSeconds(1), during: TestDuration)
        );

        var orders = Scenario.Create("get_orders", async _ =>
        {
            var request = Http.CreateRequest("GET", $"{BaseUrl}/api/v1/orders?pageNumber=1&pageSize=10")
                .WithHeader("Authorization", $"Bearer {token}");

            var response = await Http.Send(httpClient, request);
            return CreateResponse(response.StatusCode);
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: RequestRatePerSecond, interval: TimeSpan.FromSeconds(1), during: TestDuration)
        );

        var stats = NBomberRunner
            .RegisterScenarios(products, orders)
            .Run();

        var productStats = stats.ScenarioStats.First(s => s.ScenarioName == "get_products");
        var orderStats = stats.ScenarioStats.First(s => s.ScenarioName == "get_orders");

        Assert.Equal(0, productStats.Fail.Request.Count);
        Assert.Equal(0, orderStats.Fail.Request.Count);
        Assert.True(productStats.Ok.Latency.Percent95 < 500, $"Products p95 latency {productStats.Ok.Latency.Percent95}ms exceeded 500ms");
        Assert.True(orderStats.Ok.Latency.Percent95 < 500, $"Orders p95 latency {orderStats.Ok.Latency.Percent95}ms exceeded 500ms");
    }
}
