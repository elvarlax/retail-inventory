using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.Models;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class OrderGeneratorTests
{
    private static async Task SeedAsync(RetailDbContext db, int customers = 3, int products = 3)
    {
        for (int i = 0; i < customers; i++)
            db.Customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = "First",
                LastName = $"Last{i}",
                Email = $"customer{i}@test.com"
            });

        for (int i = 0; i < products; i++)
            db.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product{i}",
                SKU = $"SKU-{i}",
                Price = 10m * (i + 1),
                StockQuantity = 100
            });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrow_WhenNoCustomersOrProducts()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var generator = new OrderGenerator(db);

        // Act
        var act = async () => await generator.GenerateAsync(10);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Customers and products must exist*");
    }

    [Fact]
    public async Task GenerateAsync_ShouldInsertRequestedCount()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        await SeedAsync(db);

        var generator = new OrderGenerator(db);

        // Act
        await generator.GenerateAsync(20);

        // Assert
        db.Orders.Count().Should().Be(20);
    }

    [Fact]
    public async Task GenerateAsync_ShouldSetCorrectTotalAmount()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        await SeedAsync(db);

        var generator = new OrderGenerator(db);

        // Act
        await generator.GenerateAsync(10);

        // Assert
        var orders = await db.Orders.Include(o => o.OrderItems).ToListAsync();
        orders.Should().AllSatisfy(o =>
            o.TotalAmount.Should().Be(o.OrderItems.Sum(i => i.Quantity * i.UnitPrice)));
    }
}
