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
        var orderRepository = new OrderRepository(db);
        var customerRepository = new CustomerRepository(db);
        var productRepository = new ProductRepository(db);
        var mapper = CreateMapper();

        return new OrderService(
            orderRepository,
            customerRepository,
            productRepository,
            mapper);
    }

    #endregion

    #region Create

    [Fact]
    public async Task CreateAsync_ShouldCreateMultiItemOrder_AndDecreaseStock()
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

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            Name = "Phone",
            SKU = "SKU-1",
            Price = 100m,
            StockQuantity = 10
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 2,
            Name = "Tablet",
            SKU = "SKU-2",
            Price = 200m,
            StockQuantity = 10
        };

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

        (await db.Products.FirstAsync(p => p.Id == product1.Id))
            .StockQuantity.Should().Be(8);

        (await db.Products.FirstAsync(p => p.Id == product2.Id))
            .StockQuantity.Should().Be(9);
    }

    #endregion

    #region StateTransitions

    [Fact]
    public async Task CompleteAsync_ShouldChangeStatus()
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

        var orderId = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
            {
                new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 }
            }
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
            StockQuantity = 100
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        for (int i = 0; i < 15; i++)
        {
            await service.CreateAsync(new CreateOrderRequest
            {
                CustomerId = customer.Id,
                Items =
                {
                    new CreateOrderItemRequest { ProductId = product.Id, Quantity = 1 }
                }
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

        // Create 3 orders
        var o1 = await service.CreateAsync(new CreateOrderRequest
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

        var o2 = await service.CreateAsync(new CreateOrderRequest
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
            Name = "Item",
            SKU = "SKU-1",
            Price = 10m,
            StockQuantity = 100
        };

        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Create orders with different quantities
        await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
        {
            new CreateOrderItemRequest
            {
                ProductId = product.Id,
                Quantity = 5   // total = 50
            }
        }
        });

        await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
        {
            new CreateOrderItemRequest
            {
                ProductId = product.Id,
                Quantity = 1   // total = 10
            }
        }
        });

        // Act
        var result = await service.GetPagedAsync(
            1,
            10,
            null,
            "totalAmount",
            "asc");

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].TotalAmount.Should().Be(10m);
        result.Items[1].TotalAmount.Should().Be(50m);
    }

    #endregion

    #region GenerateRandomOrders

    [Fact]
    public async Task GenerateRandomOrdersAsync_ShouldCreateOrders_WhenDataExists()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

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
                Price = 50m * i,
                StockQuantity = 100
            }).ToList();

        db.Customers.AddRange(customers);
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        await service.GenerateRandomOrdersAsync(10);

        // Assert
        var orderCount = await db.Orders.CountAsync();
        orderCount.Should().Be(10);

        var orders = await db.Orders.Include(o => o.OrderItems).ToListAsync();
        orders.Should().OnlyContain(o => o.OrderItems.Count > 0);
    }

    [Fact]
    public async Task GenerateRandomOrdersAsync_ShouldDoNothing_WhenNoData()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var service = CreateService(db);

        // Act
        await service.GenerateRandomOrdersAsync(5);

        // Assert
        (await db.Orders.CountAsync()).Should().Be(0);
    }

    #endregion
}