using FluentAssertions;
using RetailInventory.Api.DTOs;
using RetailInventory.Application.Common.DTOs;
using RetailInventory.Application.Products.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace RetailInventory.Tests.Integration;

public class ProductsIntegrationTests : IntegrationTestBase
{
    public ProductsIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn201_WithLocation()
    {
        var client = await CreateFreshAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Name = "Laptop",
            SKU = "ELEC-LAP-001",
            Price = 999.99m,
            StockQuantity = 20
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProductById_ShouldReturn200_WhenFound()
    {
        var client = await CreateFreshAdminClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Name = "Headphones",
            SKU = "ELEC-HP-001",
            Price = 149.99m,
            StockQuantity = 30
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = createResponse.Headers.Location!.Segments.Last();

        var response = await client.GetAsync($"/api/v1/products/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Headphones");
        product.SKU.Should().Be("ELEC-HP-001");
        product.Price.Should().Be(149.99m);
        product.StockQuantity.Should().Be(30);
    }

    [Fact]
    public async Task GetProductById_ShouldReturn404_WhenNotFound()
    {
        var client = await CreateFreshAdminClientAsync();

        var response = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProducts_ShouldReturnPagedResults()
    {
        var client = await CreateFreshAdminClientAsync();

        for (var i = 1; i <= 5; i++)
        {
            await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
            {
                Name = $"Product {i:D2}",
                SKU = $"SKU-{i:D2}",
                Price = i * 10m,
                StockQuantity = 10
            });
        }

        var response = await client.GetAsync("/api/v1/products?pageNumber=1&pageSize=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(3);
    }

    [Fact]
    public async Task GetProducts_Unauthenticated_ShouldReturn401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
