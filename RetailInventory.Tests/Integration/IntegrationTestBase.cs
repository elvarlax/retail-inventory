using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RetailInventory.Api.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Tests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory _factory;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    protected HttpClient CreateClient()
    {
        return _factory.CreateClient();
    }

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient();

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await loginResponse
            .Content
            .ReadFromJsonAsync<AuthenticationResponseDto>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", content!.AccessToken);

        return client;
    }

    protected Task<HttpClient> CreateAdminClientAsync()
    {
        return CreateAuthenticatedClientAsync("admin@local", "Admin123!");
    }

    protected Task<HttpClient> CreateUserClientAsync()
    {
        return CreateAuthenticatedClientAsync("user@local", "User123!");
    }

    protected async Task<HttpClient> CreateFreshAdminClientAsync()
    {
        ResetDatabase();
        return await CreateAdminClientAsync();
    }

    protected async Task<HttpClient> CreateFreshUserClientAsync()
    {
        ResetDatabase();
        return await CreateUserClientAsync();
    }

    protected void ResetDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        DataSeeder.SeedUsers(db);
    }
}