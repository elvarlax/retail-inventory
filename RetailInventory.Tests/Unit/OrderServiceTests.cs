using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Mappings;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using RetailInventory.Api.Services;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class OrderServiceTests
{
    #region Helpers

    private IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return config.CreateMapper();
    }

    private OrderService CreateService(RetailDbContext db)
    {
        return new OrderService(
            new OrderRepository(db),
            new CustomerRepository(db),
            new ProductRepository(db),
            CreateMapper());
    }

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

    #region Create

    [Fact]
    public async Task CreateAsync_ShouldCreateMultiItemOrder_AndDecreaseStock()
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

        var service = CreateService(db);

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
            {
                new CreateOrderItemRequest { ProductId = product1.Id, Quantity = 2 },
                new CreateOrderItemRequest { ProductId = product2.Id, Quantity = 1 }
            }
        };

        // Act
        var orderId = await service.CreateAsync(request);

        // Assert
        var order = await db.Orders.Include(o => o.OrderItems)
                                   .FirstAsync(o => o.Id == orderId);

        order.OrderItems.Should().HaveCount(2);
        order.TotalAmount.Should().Be(2 * 100m + 1 * 200m);

        (await db.Products.FirstAsync(p => p.Id == product1.Id)).StockQuantity.Should().Be(8);
        (await db.Products.FirstAsync(p => p.Id == product2.Id)).StockQuantity.Should().Be(9);
    }

    #endregion

    #region StateTransitions

    [Fact]
    public async Task CompleteAsync_ShouldChangeStatus()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = MakeCustomer();
        var product = MakeProduct("SKU-1", 100m, 10);

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var orderId = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
        });

        // Act
        await service.CompleteAsync(orderId);

        // Assert
        var order = await db.Orders.FirstAsync(o => o.Id == orderId);
        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();
    }

    #endregion

    #region Queries

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPage()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = MakeCustomer();
        var product = MakeProduct("SKU-1", 100m, 100);

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        for (int i = 0; i < 15; i++)
        {
            await service.CreateAsync(new CreateOrderRequest
            {
                CustomerId = customer.Id,
                Items = { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
            });
        }

        // Act
        var result = await service.GetPagedAsync(2, 5, null, null, null);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPage_WithStatusFilter()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = MakeCustomer();
        var product = MakeProduct("SKU-1", 100m, 10);

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
        });

        var o2 = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }
        });

        await service.CompleteAsync(o2);

        // Act
        var result = await service.GetPagedAsync(1, 10, "Completed", null, null);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldRespectSorting_ByTotalAmountAscending()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = MakeCustomer();
        var product = MakeProduct("SKU-1", 10m, 100);

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 5 } }  // total = 50
        });

        await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = { new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 } }  // total = 10
        });

        // Act
        var result = await service.GetPagedAsync(1, 10, null, "totalAmount", "asc");

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].TotalAmount.Should().Be(10m);
        result.Items[1].TotalAmount.Should().Be(50m);
    }

    #endregion
}
