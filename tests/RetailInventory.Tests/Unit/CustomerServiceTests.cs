using AutoMapper;
using FluentAssertions;
using RetailInventory.Application.Authentication;
using RetailInventory.Application.Customers.Commands;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Mappings;
using RetailInventory.Application.Outbox;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Data;
using RetailInventory.Infrastructure.Repositories;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class CustomerHandlerTests
{
    #region Helpers

    private sealed class FakeOutboxRepository : IOutboxRepository
    {
        public Task AddAsync(OutboxEntry entry) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string CreateToken(User user, Guid? customerId = null) => "test-token";
    }

    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<ApplicationMappingProfile>()).CreateMapper();

    private static CreateCustomerHandler CreateCustomerHandler(RetailDbContext db) =>
        new(new CustomerRepository(db), new FakeOutboxRepository(), CreateMapper());

    private static RegisterHandler CreateRegisterHandler(RetailDbContext db) =>
        new(new UserRepository(db),
            new CustomerRepository(db),
            new FakeOutboxRepository(),
            new FakePasswordHasher(),
            new FakeTokenService());

    private static LoginHandler CreateLoginHandler(RetailDbContext db) =>
        new(new UserRepository(db),
            new CustomerRepository(db),
            new FakePasswordHasher(),
            new FakeTokenService());

    #endregion

    #region CreateCustomer

    [Fact]
    public async Task CreateCustomer_ShouldPersistCustomer()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var handler = CreateCustomerHandler(db);

        // Act
        var result = await handler.Handle(new CreateCustomerCommand("John", "Doe", "john@test.com"));

        // Assert
        var customer = await db.Customers.FindAsync(result.Id);
        customer.Should().NotBeNull();
        customer!.FirstName.Should().Be("John");
        customer.Email.Should().Be("john@test.com");
    }

    #endregion

    #region Register

    [Fact]
    public async Task Register_ShouldCreateCustomerAndUser_AndReturnToken()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var handler = CreateRegisterHandler(db);

        // Act
        var result = await handler.Handle(
            new RegisterCommand("Jane", "Doe", "jane@test.com", "password123"));

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test-token");
        result.Role.Should().Be("User");

        db.Customers.Should().ContainSingle(c => c.Email == "jane@test.com");
        db.Users.Should().ContainSingle(u => u.Email == "jane@test.com");
    }

    [Fact]
    public async Task Register_ShouldThrow_WhenEmailAlreadyExists()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var handler = CreateRegisterHandler(db);
        await handler.Handle(new RegisterCommand("Jane", "Doe", "jane@test.com", "password123"));

        // Act
        var act = async () => await handler.Handle(
            new RegisterCommand("Jane2", "Doe2", "jane@test.com", "password456"));

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*already in use*");
    }

    #endregion

    #region Login

    [Fact]
    public async Task Login_ShouldReturnToken_WithValidCredentials()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var registerHandler = CreateRegisterHandler(db);
        await registerHandler.Handle(new RegisterCommand("Jane", "Doe", "jane@test.com", "mypassword"));

        var loginHandler = CreateLoginHandler(db);

        // Act
        var result = await loginHandler.Handle(new LoginCommand("jane@test.com", "mypassword"));

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("test-token");
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WithWrongPassword()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var registerHandler = CreateRegisterHandler(db);
        await registerHandler.Handle(new RegisterCommand("Jane", "Doe", "jane@test.com", "correctpassword"));

        var loginHandler = CreateLoginHandler(db);

        // Act
        var result = await loginHandler.Handle(new LoginCommand("jane@test.com", "wrongpassword"));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var handler = CreateLoginHandler(db);

        // Act
        var result = await handler.Handle(new LoginCommand("nobody@test.com", "password"));

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
