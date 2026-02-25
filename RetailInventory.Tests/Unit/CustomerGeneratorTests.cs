using FluentAssertions;
using RetailInventory.Api.Data;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class CustomerGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldInsertRequestedCount()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var generator = new CustomerGenerator(db);

        // Act
        await generator.GenerateAsync(10);

        // Assert
        db.Customers.Count().Should().Be(10);
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateUniqueEmails()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var generator = new CustomerGenerator(db);

        // Act
        await generator.GenerateAsync(50);

        // Assert
        var emails = db.Customers.Select(c => c.Email).ToList();
        emails.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GenerateAsync_ShouldPopulateRequiredFields()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var generator = new CustomerGenerator(db);

        // Act
        await generator.GenerateAsync(5);

        // Assert
        db.Customers.ToList().Should().AllSatisfy(c =>
        {
            c.FirstName.Should().NotBeNullOrWhiteSpace();
            c.LastName.Should().NotBeNullOrWhiteSpace();
            c.Email.Should().NotBeNullOrWhiteSpace();
        });
    }
}
