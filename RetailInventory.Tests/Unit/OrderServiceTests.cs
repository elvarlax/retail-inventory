using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Exceptions;
using RetailInventory.Api.Mappings;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using RetailInventory.Api.Services;
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

    private OrderService CreateService(RetailDbContext db)
    {
        var repository = new OrderRepository(db);
        var mapper = CreateMapper();
        return new OrderService(db, repository, mapper);
    }

    #region Create

    [Fact]
    public async Task CreateAsync_ShouldCreateOrder_AndDecreaseStock()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

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

        var service = CreateService(db);

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
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var service = CreateService(db);

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

        // Act
        Func<Task> act = async () => await service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenStockIsInsufficient()
    {
        // Arrange
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

        var service = CreateService(db);

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

        // Act
        Func<Task> act = async () => await service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenQuantityIsZero()
    {
        // Arrange
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

        var service = CreateService(db);

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
            {
                new CreateOrderItemRequest
                {
                    ProductId = product.Id,
                    Quantity = 0
                }
            }
        };

        // Act
        Func<Task> act = async () => await service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenItemsEmpty()
    {
        // Arrange
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

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = new List<CreateOrderItemRequest>() // empty list
        };

        // Act
        Func<Task> act = async () => await service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion

    #region StateTransitions

    [Fact]
    public async Task CompleteAsync_ShouldThrow_WhenOrderAlreadyCompleted()
    {
        // Arrange
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

        var service = CreateService(db);

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

        // Act
        Func<Task> act = async () => await service.CompleteAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrow_WhenOrderNotFound()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var service = CreateService(db);

        // Act
        Func<Task> act = async () => await service.CompleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CancelAsync_ShouldRestoreStock_AndMarkAsCancelled()
    {
        // Arrange
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

        var service = CreateService(db);

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

        // Act
        await service.CancelAsync(orderId);

        // Assert
        var order = await db.Orders.Include(o => o.OrderItems)
                                   .FirstAsync(o => o.Id == orderId);

        var updatedProduct = await db.Products.FirstAsync();

        order.Status.Should().Be(OrderStatus.Cancelled);
        updatedProduct.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrow_WhenOrderAlreadyCompleted()
    {
        // Arrange
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

        var service = CreateService(db);

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

        // Act
        Func<Task> act = async () => await service.CancelAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CancelAsync_ShouldThrow_WhenOrderNotFound()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var service = CreateService(db);

        // Act
        Func<Task> act = async () => await service.CancelAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region Queries

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenNotFound()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var service = CreateService(db);

        // Act
        Func<Task> act = async () => await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldThrow_WhenStatusInvalid()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var service = CreateService(db);

        // Act
        Func<Task> act = async () =>
            await service.GetPagedAsync(1, 10, "InvalidStatus");

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldReturnCorrectAggregation()
    {
        // Arrange
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
            StockQuantity = 10
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var pendingId = await service.CreateAsync(new CreateOrderRequest
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

        var completedId = await service.CreateAsync(new CreateOrderRequest
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

        await service.CompleteAsync(completedId);

        var cancelledId = await service.CreateAsync(new CreateOrderRequest
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

        await service.CancelAsync(cancelledId);

        // Act
        var summary = await service.GetSummaryAsync();

        // Assert
        summary.TotalOrders.Should().Be(3);
        summary.PendingOrders.Should().Be(1);
        summary.CompletedOrders.Should().Be(1);
        summary.CancelledOrders.Should().Be(1);
        summary.TotalRevenue.Should().Be(100m);
        summary.PendingRevenue.Should().Be(100m);
    }

    #endregion
}