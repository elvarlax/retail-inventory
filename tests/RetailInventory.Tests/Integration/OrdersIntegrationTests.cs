using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetailInventory.Infrastructure.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Domain;

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

        var response = await client.GetAsync("/api/v1/orders");

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
            FirstName = "Integration",
            LastName = "User",
            Email = "integration@test.com"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
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
                new CreateOrderItemRequest { ProductId = product.Id, Quantity = 2 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        result.Should().NotBeNull();

        var orderId = result!.OrderId;

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var updatedProduct = await verificationDb.Products.FirstAsync(p => p.Id == product.Id);
        updatedProduct.StockQuantity.Should().Be(3);

        var order = await verificationDb.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Pending);
        order.CompletedAt.Should().BeNull();
        order.OrderItems.Should().HaveCount(1);
        order.TotalAmount.Should().Be(200m);
    }


    [Fact]
    public async Task CreateOrder_WithZeroQuantity_ShouldReturnBadRequest()
    {
        var client = await CreateFreshUserClientAsync();

        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items =
            {
                new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 0 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyItems_ShouldReturnBadRequest()
    {
        var client = await CreateFreshUserClientAsync();

        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items = []
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task CompleteOrder_ShouldSetCompletedAt()
    {
        var userClient = await CreateFreshUserClientAsync();
        var adminClient = await CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Integration",
            LastName = "User",
            Email = "integration@test.com"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "IntegrationPhone",
            SKU = "INT-1",
            Price = 100m,
            StockQuantity = 10
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var createResponse = await userClient.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
            {
                new CreateOrderItemRequest { ProductId = product.Id, Quantity = 2 }
            }
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();
        var orderId = createResult!.OrderId;

        var completeResponse = await adminClient.PostAsync($"/api/v1/orders/{orderId}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var order = await verifyDb.Orders.FirstAsync(o => o.Id == orderId);

        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();

        var updatedProduct = await verifyDb.Products.FirstAsync(p => p.Id == product.Id);
        updatedProduct.StockQuantity.Should().Be(8);
    }
}
