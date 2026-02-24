using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetailInventory.Api.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;

namespace RetailInventory.Tests.Integration;

public class OrdersIntegrationTests : IntegrationTestBase
{
    public OrdersIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetOrders_ShouldReturnOk()
    {
        var client = await CreateFreshUserClientAsync();

        var response = await client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateOrder_ShouldCreateOrder_AndDecreaseStock()
    {
        var client = await CreateFreshUserClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            FirstName = "Integration",
            LastName = "User",
            Email = "integration@test.com"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            Name = "IntegrationPhone",
            SKU = "INT-1",
            Price = 100m,
            StockQuantity = 5
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
            {
                new CreateOrderItemRequest
                {
                    ProductId = product.Id,
                    Quantity = 2
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        result.Should().NotBeNull();

        var orderId = result!.OrderId;

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var updatedProduct = await verificationDb.Products.FirstAsync();
        updatedProduct.StockQuantity.Should().Be(3);

        var order = await verificationDb.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        order.Should().NotBeNull();
        order!.OrderItems.Should().HaveCount(1);
        order.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public async Task GenerateOrders_ShouldCreateRequestedAmount()
    {
        var client = await CreateFreshUserClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var customers = Enumerable.Range(1, 3)
            .Select(i => new Customer
            {
                Id = Guid.NewGuid(),
                ExternalId = i,
                FirstName = $"User{i}",
                LastName = "Test",
                Email = $"user{i}@test.com"
            }).ToList();

        var products = Enumerable.Range(1, 5)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                ExternalId = i,
                Name = $"Product{i}",
                SKU = $"SKU-{i}",
                Price = 10m * i,
                StockQuantity = 100
            }).ToList();

        db.Customers.AddRange(customers);
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        var response = await client.PostAsJsonAsync(
            "/api/orders/generate",
            new GenerateOrdersRequest { Count = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(5);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var totalOrders = await verificationDb.Orders.CountAsync();
        totalOrders.Should().Be(5);
    }
}