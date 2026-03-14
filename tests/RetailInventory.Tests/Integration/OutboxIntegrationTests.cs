using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetailInventory.Infrastructure.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Application.Customers.Events;
using RetailInventory.Application.Orders.Events;
using RetailInventory.Application.Outbox;
using RetailInventory.Application.Products.Events;
using RetailInventory.Domain;

namespace RetailInventory.Tests.Integration;

public class OutboxIntegrationTests : IntegrationTestBase
{
    public OutboxIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Register_ShouldEmitCustomerCreatedV1_InOutbox()
    {
        ResetDatabase();
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@test.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var message = await db.OutboxMessages
            .FirstOrDefaultAsync(m => m.Type == nameof(CustomerCreatedV1));

        message.Should().NotBeNull();
        message!.Source.Should().Be(OutboxConstants.Source);
        message.PublishedAtUtc.Should().BeNull();
        message.Payload.Should().Contain("jane.doe@test.com");
    }

    [Fact]
    public async Task CreateProduct_ShouldEmitProductCreatedV1_InOutbox()
    {
        var client = await CreateFreshAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Name = "Test Widget",
            SKU = "WDGT-001",
            Price = 29.99m,
            StockQuantity = 100
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var message = await db.OutboxMessages
            .FirstOrDefaultAsync(m => m.Type == nameof(ProductCreatedV1));

        message.Should().NotBeNull();
        message!.Source.Should().Be(OutboxConstants.Source);
        message.PublishedAtUtc.Should().BeNull();
        message.Payload.Should().Contain("Test Widget");
        message.Payload.Should().Contain("WDGT-001");
    }

    [Fact]
    public async Task CreateOrder_ShouldEmitOrderPlacedV1_InOutbox()
    {
        var client = await CreateFreshUserClientAsync();

        using var seedScope = _factory.Services.CreateScope();
        var db = seedScope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Outbox",
            LastName = "Test",
            Email = "outbox.order@test.com"
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Outbox Product",
            SKU = "OBX-001",
            Price = 49.99m,
            StockQuantity = 10
        };
        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var response = await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 2 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();

        using var scope = _factory.Services.CreateScope();
        var verifyDb = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var message = await verifyDb.OutboxMessages
            .FirstOrDefaultAsync(m => m.Type == nameof(OrderPlacedV1));

        message.Should().NotBeNull();
        message!.Source.Should().Be(OutboxConstants.Source);
        message.PublishedAtUtc.Should().BeNull();
        message.Payload.Should().Contain(created!.OrderId.ToString());
        message.Payload.Should().Contain(customer.Id.ToString());
    }

    [Fact]
    public async Task CompleteOrder_ShouldEmitOrderStatusChangedV1_InOutbox()
    {
        var userClient = await CreateFreshUserClientAsync();
        var adminClient = await CreateAdminClientAsync();

        using var seedScope = _factory.Services.CreateScope();
        var db = seedScope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Outbox",
            LastName = "Complete",
            Email = "outbox.complete@test.com"
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Complete Product",
            SKU = "CMP-001",
            Price = 75m,
            StockQuantity = 10
        };
        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var createResponse = await userClient.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        var completeResponse = await adminClient.PostAsync($"/api/v1/orders/{created!.OrderId}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var verifyDb = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

        var messages = await verifyDb.OutboxMessages.ToListAsync();

        messages.Should().Contain(m => m.Type == nameof(OrderPlacedV1));

        var statusMessage = messages.FirstOrDefault(m => m.Type == nameof(OrderStatusChangedV1));
        statusMessage.Should().NotBeNull();
        statusMessage!.Payload.Should().Contain(created.OrderId.ToString());
        statusMessage.Payload.Should().Contain("Completed");
    }
}
