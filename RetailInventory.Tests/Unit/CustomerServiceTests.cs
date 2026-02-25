using AutoMapper;
using FluentAssertions;
using RetailInventory.Api.Data;
using RetailInventory.Api.Mappings;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using RetailInventory.Api.Services;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class CustomerServiceTests
{
    private IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return config.CreateMapper();
    }

    private CustomerService CreateService(RetailDbContext db)
    {
        return new CustomerService(new CustomerRepository(db), CreateMapper());
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
    public async Task GetByIdAsync_ShouldReturnMappedCustomer()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetByIdAsync(customer.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldRespectSorting_ByFirstNameDescending()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Customers.AddRange(
            new Customer { Id = Guid.NewGuid(), FirstName = "Anna",  LastName = "Z", Email = "a@test.com" },
            new Customer { Id = Guid.NewGuid(), FirstName = "Bjorn", LastName = "Y", Email = "b@test.com" },
            new Customer { Id = Guid.NewGuid(), FirstName = "Carl",  LastName = "X", Email = "c@test.com" }
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetPagedAsync(1, 10, "firstName", "desc");

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].FirstName.Should().Be("Carl");
        result.Items[1].FirstName.Should().Be("Bjorn");
        result.Items[2].FirstName.Should().Be("Anna");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPage()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        for (int i = 0; i < 12; i++)
            db.Customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = "First",
                LastName = $"Last{i:D2}",
                Email = $"customer{i}@test.com"
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
    public async Task GetPagedAsync_ShouldRespectSorting_ByLastNameAscending()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Customers.AddRange(
            new Customer { Id = Guid.NewGuid(), FirstName = "A", LastName = "Zebra",  Email = "z@test.com" },
            new Customer { Id = Guid.NewGuid(), FirstName = "B", LastName = "Apple",  Email = "a@test.com" },
            new Customer { Id = Guid.NewGuid(), FirstName = "C", LastName = "Mango",  Email = "m@test.com" }
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetPagedAsync(1, 10, null, "asc");

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].LastName.Should().Be("Apple");
        result.Items[1].LastName.Should().Be("Mango");
        result.Items[2].LastName.Should().Be("Zebra");
    }
}
