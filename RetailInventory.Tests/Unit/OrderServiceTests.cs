using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Exceptions;
using RetailInventory.Api.Models;
using RetailInventory.Api.Services;
using RetailInventory.Api.Mappings;
using AutoMapper;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class OrderServiceTests
{
    private IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return config.CreateMapper();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateOrder_AndDecreaseStock()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn; // ensure connection is disposed at end

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            FirstName = "Test",
            LastName = "Customer",
            Email = "test.customer@example.com"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 100,
            Name = "Phone",
            SKU = "SKU-100",
            Price = 199.99m,
            StockQuantity = 5
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var mapper = CreateMapper();
        var service = new OrderService(db, mapper);

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

        // Act
        var orderId = await service.CreateAsync(request);

        // Assert
        var order = await db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(199.99m * 2);

        var updatedProduct = await db.Products.FirstAsync(p => p.Id == product.Id);
        updatedProduct.StockQuantity.Should().Be(3);

        order.OrderItems.Should().HaveCount(1);
        order.OrderItems[0].Quantity.Should().Be(2);
        order.OrderItems[0].UnitPrice.Should().Be(199.99m);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCustomerDoesNotExist()
    {
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var mapper = CreateMapper();
        var service = new OrderService(db, mapper);

        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items =
            {
                new CreateOrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            }
        };

        Func<Task> act = async () => await service.CreateAsync(request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenStockIsInsufficient()
    {
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "user@test.com"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            Name = "Phone",
            SKU = "SKU-1",
            Price = 100m,
            StockQuantity = 1
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var mapper = CreateMapper();
        var service = new OrderService(db, mapper);

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
        {
            new CreateOrderItemRequest
            {
                ProductId = product.Id,
                Quantity = 5
            }
        }
        };

        Func<Task> act = async () => await service.CreateAsync(request);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CancelAsync_ShouldRestoreStock_AndMarkAsCancelled()
    {
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            Name = "Laptop",
            SKU = "SKU-1",
            Price = 500m,
            StockQuantity = 10
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var mapper = CreateMapper();
        var service = new OrderService(db, mapper);

        // Create order first
        var orderId = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
        {
            new CreateOrderItemRequest
            {
                ProductId = product.Id,
                Quantity = 3
            }
        }
        });

        var stockAfterCreate = (await db.Products.FirstAsync()).StockQuantity;
        stockAfterCreate.Should().Be(7);

        // Act
        await service.CancelAsync(orderId);

        var order = await db.Orders.Include(o => o.OrderItems)
                                   .FirstAsync(o => o.Id == orderId);

        var updatedProduct = await db.Products.FirstAsync();

        order.Status.Should().Be(OrderStatus.Cancelled);
        updatedProduct.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrow_WhenOrderAlreadyCompleted()
    {
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            Name = "Phone",
            SKU = "SKU-1",
            Price = 100m,
            StockQuantity = 5
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var mapper = CreateMapper();
        var service = new OrderService(db, mapper);

        var orderId = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
        {
            new CreateOrderItemRequest
            {
                ProductId = product.Id,
                Quantity = 1
            }
        }
        });

        await service.CompleteAsync(orderId);

        Func<Task> act = async () => await service.CompleteAsync(orderId);

        await act.Should().ThrowAsync<BadRequestException>();
    }
}