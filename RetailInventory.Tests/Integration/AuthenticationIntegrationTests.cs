using FluentAssertions;
using RetailInventory.Api.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace RetailInventory.Tests.Integration;

public class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        ResetDatabase();

        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login",
            new LoginRequestDto
            {
                Email = "admin@local",
                Password = "Admin123!"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<AuthenticationResponseDto>();

        content.Should().NotBeNull();
        content!.TokenType.Should().Be("Bearer");
        content.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AdminEndpoint_WithUserRole_ReturnsForbidden()
    {
        var client = await CreateFreshUserClientAsync();

        var response = await client.GetAsync("/admin/secret");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_ReturnsOk()
    {
        var client = await CreateFreshAdminClientAsync();

        var response = await client.GetAsync("/admin/secret");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}