using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.Commands;
using RetailInventory.Application.Outbox;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Data;
using RetailInventory.Infrastructure.Repositories;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class OrderHandlerTests
{
    #region Helpers

    private sealed class FakeOutboxRepository : IOutboxRepository
    {
        public Task AddAsync(OutboxEntry entry) => Task.CompletedTask;
    }

    private static PlaceOrderHandler CreatePlaceHandler(RetailDbContext db) =>
        new(new OrderRepository(db),
            new ProductRepository(db),
            new CustomerRepository(db),
            new FakeOutboxRepository());

    private static CompleteOrderHandler CreateCompleteHandler(RetailDbContext db) =>
        new(new OrderRepository(db), new FakeOutboxRepository());

    private static CancelOrderHandler CreateCancelHandler(RetailDbContext db) =>
        new(new OrderRepository(db),
            new ProductRepository(db),
            new FakeOutboxRepository());

    private static Customer MakeCustomer(string email = "test@test.com") => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        Email = email
    };

    private static Product MakeProduct(string sku, decimal price, int stock = 10) => new()
    {
        Id = Guid.NewGuid(),
        Name = sku,
        SKU = sku,
        Price = price,
        StockQuantity = stock
    };

    #endregion

    #region PlaceOrder

    [Fact]
    public async Task PlaceOrder_ShouldCreateMultiItemOrder_AndDecreaseStock()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = MakeCustomer();
        var product1 = MakeProduct("SKU-1", 100m, 10);
        var product2 = MakeProduct("SKU-2", 200m, 10);

        db.Customers.Add(customer);
        db.Products.AddRange(product1, product2);
        await db.SaveChangesAsync();

        var handler = CreatePlaceHandler(db);
        var command = new PlaceOrderCommand(
            CustomerId: customer.Id,
            Items:
            [
                new OrderItemRequest(product1.Id, 2),
                new OrderItemRequest(product2.Id, 1)
            ]);

        // Act
        var orderId = await handler.Handle(command);

        // Assert
        var order = await db.Orders.Include(o => o.OrderItems)
                                   .FirstAsync(o => o.Id == orderId);

        order.OrderItems.Should().HaveCount(2);
        order.TotalAmount.Should().Be(2 * 100m + 1 * 200m);

        (await db.Products.FirstAsync(p => p.Id == product1.Id)).StockQuantity.Should().Be(8);
        (await db.Products.FirstAsync(p => p.Id == product2.Id)).StockQuantity.Should().Be(9);
    }

    [Fact]
    public async Task PlaceOrder_ShouldThrow_WhenInsufficientStock()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = MakeCustomer();
        var product = MakeProduct("SKU-1", 100m, stock: 1);

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = CreatePlaceHandler(db);
        var command = new PlaceOrderCommand(
            CustomerId: customer.Id,
            Items: [new OrderItemRequest(product.Id, 5)]);

        // Act
        var act = async () => await handler.Handle(command);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*Insufficient stock*");
    }

    #endregion

    #region StateTransitions

    [Fact]
    public async Task CompleteOrder_ShouldChangeStatus()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = MakeCustomer();
        var product = MakeProduct("SKU-1", 100m, 10);

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var orderId = await CreatePlaceHandler(db).Handle(new PlaceOrderCommand(
            customer.Id, [new OrderItemRequest(product.Id, 1)]));

        // Act
        await CreateCompleteHandler(db).Handle(new CompleteOrderCommand(orderId));

        // Assert
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelOrder_ShouldRestoreStock()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = MakeCustomer();
        var product = MakeProduct("SKU-1", 100m, 10);

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var orderId = await CreatePlaceHandler(db).Handle(new PlaceOrderCommand(
            customer.Id, [new OrderItemRequest(product.Id, 3)]));

        // Act
        await CreateCancelHandler(db).Handle(new CancelOrderCommand(orderId));

        // Assert
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        order.Status.Should().Be(OrderStatus.Cancelled);

        (await db.Products.FirstAsync(p => p.Id == product.Id)).StockQuantity.Should().Be(10);
    }

    #endregion
}
