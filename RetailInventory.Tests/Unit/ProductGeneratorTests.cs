using FluentAssertions;
using RetailInventory.Api.Data;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class ProductGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldInsertRequestedCount()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var generator = new ProductGenerator(db);

        // Act
        await generator.GenerateAsync(10);

        // Assert
        db.Products.Count().Should().Be(10);
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateUniqueSku()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var generator = new ProductGenerator(db);

        // Act
        await generator.GenerateAsync(50);

        // Assert
        var skus = db.Products.Select(p => p.SKU).ToList();
        skus.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GenerateAsync_ShouldGeneratePositivePrices()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var generator = new ProductGenerator(db);

        // Act
        await generator.GenerateAsync(10);

        // Assert
        db.Products.ToList().Should().AllSatisfy(p =>
            p.Price.Should().BeGreaterThan(0m));
    }
}
