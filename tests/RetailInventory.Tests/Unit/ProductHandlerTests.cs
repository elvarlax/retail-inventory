using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Mappings;
using RetailInventory.Application.Outbox;
using RetailInventory.Application.Products.Commands;
using RetailInventory.Application.Products.Events;
using RetailInventory.Application.Products.Queries;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Repositories;
using RetailInventory.Tests.Helpers;

namespace RetailInventory.Tests.Unit;

public class ProductHandlerTests
{
    #region Helpers

    private sealed class FakeOutboxRepository : IOutboxRepository
    {
        public Task AddAsync(OutboxEntry entry) => Task.CompletedTask;
    }

    private static IMapper CreateMapper() =>
        new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<ApplicationMappingProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

    private static CreateProductHandler CreateHandler(RetailInventory.Infrastructure.Data.RetailDbContext db) =>
        new(new ProductRepository(db), new FakeOutboxRepository(), CreateMapper());

    private static Product MakeProduct(string name, string sku, decimal price, int stock = 10) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        SKU = sku,
        Price = price,
        StockQuantity = stock
    };

    #endregion

    #region CreateProduct

    [Fact]
    public async Task CreateProduct_ShouldPersistProduct()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var handler = CreateHandler(db);

        // Act
        var id = await handler.Handle(new CreateProductCommand("Phone", "ELEC-001", null, 299.99m, 50), CancellationToken.None);

        // Assert
        var product = await db.Products.FindAsync(id);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Phone");
        product.SKU.Should().Be("ELEC-001");
        product.Price.Should().Be(299.99m);
        product.StockQuantity.Should().Be(50);
    }

    [Fact]
    public async Task CreateProduct_ShouldEmitOutboxEntry()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        OutboxEntry? captured = null;
        var fakeOutbox = new CapturingOutboxRepository(e => captured = e);
        var handler = new CreateProductHandler(new ProductRepository(db), fakeOutbox, CreateMapper());

        // Act
        await handler.Handle(new CreateProductCommand("Phone", "ELEC-001", null, 299.99m, 50), CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Type.Should().Be("ProductCreatedV1");
    }

    #endregion

    #region GetProductById

    [Fact]
    public async Task GetProductById_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var handler = new GetProductByIdHandler(new EfProductQueryRepository(db));

        // Act
        var result = await handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProductById_ShouldReturnDto_WhenFound()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        var product = MakeProduct("Phone", "ELEC-001", 299.99m, 50);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new GetProductByIdHandler(new EfProductQueryRepository(db));

        // Act
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be("Phone");
        result.SKU.Should().Be("ELEC-001");
        result.Price.Should().Be(299.99m);
        result.StockQuantity.Should().Be(50);
    }

    #endregion

    #region GetProducts (paging + sorting)

    [Fact]
    public async Task GetProducts_ShouldReturnCorrectPage()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        for (var i = 0; i < 12; i++)
            db.Products.Add(MakeProduct($"Product{i:D2}", $"SKU-{i:D2}", 10m));

        await db.SaveChangesAsync();

        var handler = new GetProductsHandler(new EfProductQueryRepository(db));

        // Act
        var result = await handler.Handle(new GetProductsQuery(PageNumber: 2, PageSize: 5, SortBy: null, SortDirection: null), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(12);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetProducts_ShouldSortByPriceDescending()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Products.AddRange(
            MakeProduct("Cheap",     "SKU-1", 10m),
            MakeProduct("Mid",       "SKU-2", 50m),
            MakeProduct("Expensive", "SKU-3", 100m));

        await db.SaveChangesAsync();

        var handler = new GetProductsHandler(new EfProductQueryRepository(db));

        // Act
        var result = await handler.Handle(new GetProductsQuery(1, 10, "price", "desc"), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Price.Should().Be(100m);
        result.Items[1].Price.Should().Be(50m);
        result.Items[2].Price.Should().Be(10m);
    }

    [Fact]
    public async Task GetProducts_ShouldSortByStockQuantityAscending()
    {
        // Arrange
        var (db, conn) = TestDbFactory.CreateSqliteInMemoryDb();
        await using var _ = conn;

        db.Products.AddRange(
            MakeProduct("High",   "SKU-1", 10m, stock: 100),
            MakeProduct("Low",    "SKU-2", 10m, stock: 5),
            MakeProduct("Medium", "SKU-3", 10m, stock: 30));

        await db.SaveChangesAsync();

        var handler = new GetProductsHandler(new EfProductQueryRepository(db));

        // Act
        var result = await handler.Handle(new GetProductsQuery(1, 10, "stockquantity", "asc"), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].StockQuantity.Should().Be(5);
        result.Items[1].StockQuantity.Should().Be(30);
        result.Items[2].StockQuantity.Should().Be(100);
    }

    #endregion

    #region Helpers (private)

    private sealed class CapturingOutboxRepository : IOutboxRepository
    {
        private readonly Action<OutboxEntry> _capture;
        public CapturingOutboxRepository(Action<OutboxEntry> capture) => _capture = capture;
        public Task AddAsync(OutboxEntry entry) { _capture(entry); return Task.CompletedTask; }
    }

    #endregion
}
