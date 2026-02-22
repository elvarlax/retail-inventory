using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetailInventory.Api.Data;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using RetailInventory.Api.Services;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class ProductServiceTests
{
    private readonly Mock<IDummyJsonService> _dummyMock = new();

    private ProductService CreateService(RetailDbContext db)
    {
        var repository = new ProductRepository(db);
        return new ProductService(_dummyMock.Object, repository);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedProducts()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            Name = "Phone",
            SKU = "SKU-1",
            StockQuantity = 5,
            Price = 100m
        });

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Phone");
        result[0].Price.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var service = CreateService(db);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ImportFromExternalAsync_ShouldInsertOnlyNewProducts()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 2,
            Name = "Existing",
            SKU = "SKU-2",
            StockQuantity = 5,
            Price = 200m
        });

        await db.SaveChangesAsync();

        _dummyMock.Setup(d => d.GetProductsAsync())
            .ReturnsAsync(new List<DummyJsonProduct>
            {
                new() { Id = 1, Title = "Phone", Stock = 10, Price = 100 },
                new() { Id = 2, Title = "Existing", Stock = 5, Price = 200 }
            });

        var service = CreateService(db);

        // Act
        var inserted = await service.ImportFromExternalAsync();

        // Assert
        inserted.Should().Be(1);
        db.Products.Count().Should().Be(2);
    }

    [Fact]
    public async Task ImportFromExternalAsync_ShouldReturnZero_WhenNoProducts()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        _dummyMock.Setup(d => d.GetProductsAsync())
            .ReturnsAsync(new List<DummyJsonProduct>());

        var service = CreateService(db);

        // Act
        var inserted = await service.ImportFromExternalAsync();

        // Assert
        inserted.Should().Be(0);
        db.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMappedProduct()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            Name = "Phone",
            SKU = "SKU-1",
            StockQuantity = 5,
            Price = 100m
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetByIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be("Phone");
        result.SKU.Should().Be("SKU-1");
        result.StockQuantity.Should().Be(5);
        result.Price.Should().Be(100m);
    }

    [Fact]
    public async Task ImportFromExternalAsync_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        _dummyMock.Setup(d => d.GetProductsAsync())
            .ReturnsAsync(new List<DummyJsonProduct>
            {
            new() { Id = 10, Title = "Tablet", Stock = 25, Price = 300 }
            });

        var service = CreateService(db);

        // Act
        var inserted = await service.ImportFromExternalAsync();

        // Assert
        inserted.Should().Be(1);

        var product = await db.Products.FirstAsync();

        product.ExternalId.Should().Be(10);
        product.Name.Should().Be("Tablet");
        product.SKU.Should().Be("DUMMY-10");
        product.StockQuantity.Should().Be(25);
        product.Price.Should().Be(300);
    }
}