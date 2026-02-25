using AutoMapper;
using FluentAssertions;
using RetailInventory.Api.Data;
using RetailInventory.Api.Mappings;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using RetailInventory.Api.Services;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class ProductServiceTests
{
    private IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return config.CreateMapper();
    }

    private ProductService CreateService(RetailDbContext db)
    {
        return new ProductService(new ProductRepository(db), CreateMapper());
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
    public async Task GetByIdAsync_ShouldReturnMappedProduct()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var product = new Product
        {
            Id = Guid.NewGuid(),
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
    public async Task GetPagedAsync_ShouldRespectSorting_ByPriceDescending()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Cheap",     SKU = "SKU-1", Price = 10m,  StockQuantity = 10 },
            new Product { Id = Guid.NewGuid(), Name = "Mid",       SKU = "SKU-2", Price = 50m,  StockQuantity = 10 },
            new Product { Id = Guid.NewGuid(), Name = "Expensive", SKU = "SKU-3", Price = 100m, StockQuantity = 10 }
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetPagedAsync(1, 10, "price", "desc");

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Price.Should().Be(100m);
        result.Items[1].Price.Should().Be(50m);
        result.Items[2].Price.Should().Be(10m);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPage()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        for (int i = 0; i < 12; i++)
            db.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product{i:D2}",
                SKU = $"SKU-{i:D2}",
                Price = 10m,
                StockQuantity = 5
            });

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetPagedAsync(2, 5, null, null);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(12);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldRespectSorting_ByStockQuantityAscending()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "High",   SKU = "SKU-1", Price = 10m, StockQuantity = 100 },
            new Product { Id = Guid.NewGuid(), Name = "Low",    SKU = "SKU-2", Price = 10m, StockQuantity = 5   },
            new Product { Id = Guid.NewGuid(), Name = "Medium", SKU = "SKU-3", Price = 10m, StockQuantity = 30  }
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetPagedAsync(1, 10, "stockQuantity", "asc");

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].StockQuantity.Should().Be(5);
        result.Items[1].StockQuantity.Should().Be(30);
        result.Items[2].StockQuantity.Should().Be(100);
    }
}
