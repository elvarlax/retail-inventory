using FluentAssertions;
using RetailInventory.Api.DTOs;
using RetailInventory.Application.Common.DTOs;
using RetailInventory.Application.Customers.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace RetailInventory.Tests.Integration;

public class CustomersIntegrationTests : IntegrationTestBase
{
    public CustomersIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateCustomer_ShouldReturn201_WithLocation()
    {
        var client = await CreateFreshAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest
        {
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice.smith@test.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCustomerById_ShouldReturn200_WhenFound()
    {
        var client = await CreateFreshAdminClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest
        {
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob.jones@test.com"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = createResponse.Headers.Location!.Segments.Last();

        var response = await client.GetAsync($"/api/v1/customers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        customer.Should().NotBeNull();
        customer!.FirstName.Should().Be("Bob");
        customer.LastName.Should().Be("Jones");
        customer.Email.Should().Be("bob.jones@test.com");
    }

    [Fact]
    public async Task GetCustomerById_ShouldReturn404_WhenNotFound()
    {
        var client = await CreateFreshAdminClientAsync();

        var response = await client.GetAsync($"/api/v1/customers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCustomers_ShouldReturnPagedResults()
    {
        var client = await CreateFreshAdminClientAsync();

        for (var i = 1; i <= 4; i++)
        {
            await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest
            {
                FirstName = $"User{i:D2}",
                LastName = "Test",
                Email = $"user{i:D2}@test.com"
            });
        }

        var response = await client.GetAsync("/api/v1/customers?pageNumber=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(6); // 2 seeded + 4 created
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetCustomers_Unauthenticated_ShouldReturn401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
