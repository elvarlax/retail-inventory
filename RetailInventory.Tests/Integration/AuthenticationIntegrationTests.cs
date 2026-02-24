using RetailInventory.Api.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace RetailInventory.Tests.Integration;

public class AuthenticationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@local",
            password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<AuthenticationResponseDto>();

        Assert.NotNull(content);
        Assert.False(string.IsNullOrWhiteSpace(content!.AccessToken));
    }

    [Fact]
    public async Task AdminEndpoint_WithUserRole_ReturnsForbidden()
    {
        // login as normal user
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "user@local",
            password = "User123!"
        });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                loginContent!.AccessToken
            );

        var response = await _client.GetAsync("/admin/secret");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_ReturnsOk()
    {
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@local",
            password = "Admin123!"
        });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                loginContent!.AccessToken
            );

        var response = await _client.GetAsync("/admin/secret");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}