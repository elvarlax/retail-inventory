using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetailInventory.Api.Data;
using RetailInventory.Api.Mappings;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using RetailInventory.Api.Services;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class CustomerServiceTests
{
    private readonly Mock<IDummyJsonService> _dummyMock = new();

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
        var dummyService = _dummyMock.Object;
        var customerRepository = new CustomerRepository(db);
        var mapper = CreateMapper();

        return new CustomerService(
            dummyService,
            customerRepository,
            mapper);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedCustomers()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Customers.Add(new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        });

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Email.Should().Be("john@test.com");
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
    public async Task ImportFromExternalAsync_ShouldInsertOnlyNewCustomers()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Customers.Add(new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = 2,
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@test.com"
        });

        await db.SaveChangesAsync();

        _dummyMock.Setup(d => d.GetUsersAsync())
            .ReturnsAsync(new List<DummyJsonUser>
            {
                new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com" },
                new() { Id = 2, FirstName = "Existing", LastName = "User", Email = "existing@test.com" }
            });

        var service = CreateService(db);

        // Act
        var inserted = await service.ImportFromExternalAsync();

        // Assert
        inserted.Should().Be(1);
        db.Customers.Count().Should().Be(2);
    }

    [Fact]
    public async Task ImportFromExternalAsync_ShouldReturnZero_WhenNoUsers()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        _dummyMock.Setup(d => d.GetUsersAsync())
            .ReturnsAsync(new List<DummyJsonUser>());

        var service = CreateService(db);

        // Act
        var inserted = await service.ImportFromExternalAsync();

        // Assert
        inserted.Should().Be(0);
        db.Customers.Should().BeEmpty();
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
            ExternalId = 1,
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
    public async Task ImportFromExternalAsync_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        _dummyMock.Setup(d => d.GetUsersAsync())
            .ReturnsAsync(new List<DummyJsonUser>
            {
            new()
            {
                Id = 10,
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice@test.com"
            }
            });

        var service = CreateService(db);

        // Act
        var inserted = await service.ImportFromExternalAsync();

        // Assert
        inserted.Should().Be(1);

        var customer = await db.Customers.FirstAsync();

        customer.ExternalId.Should().Be(10);
        customer.FirstName.Should().Be("Alice");
        customer.LastName.Should().Be("Smith");
        customer.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldRespectSorting_ByFirstNameDescending()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Customers.AddRange(
            new Customer
            {
                Id = Guid.NewGuid(),
                ExternalId = 1,
                FirstName = "Anna",
                LastName = "Z",
                Email = "a@test.com"
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                ExternalId = 2,
                FirstName = "Bjorn",
                LastName = "Y",
                Email = "b@test.com"
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                ExternalId = 3,
                FirstName = "Carl",
                LastName = "X",
                Email = "c@test.com"
            }
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
}