using Microsoft.EntityFrameworkCore;
using RetailInventory.Application.Customers.DTOs;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.DTOs;
using RetailInventory.Application.Products.DTOs;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Data;

namespace RetailInventory.Tests.Helpers;

// EF-backed implementations of the Dapper query repos — used in integration tests
// where NpgsqlDataSource is not available (SQLite in-memory).

public class EfProductQueryRepository : IProductQueryRepository
{
    private readonly RetailDbContext _db;
    public EfProductQueryRepository(RetailDbContext db) => _db = db;

    public Task<int> CountAsync(string? search = null)
    {
        var query = _db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        return query.CountAsync();
    }

    public async Task<List<ProductDto>> GetPagedAsync(int skip, int take, string? sortBy, string? sortDirection, string? search = null)
    {
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        query = (sortBy?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("name", "desc")          => query.OrderByDescending(p => p.Name),
            ("name", _)               => query.OrderBy(p => p.Name),
            ("price", "desc")         => query.OrderByDescending(p => p.Price),
            ("price", _)              => query.OrderBy(p => p.Price),
            ("sku", "desc")           => query.OrderByDescending(p => p.SKU),
            ("sku", _)                => query.OrderBy(p => p.SKU),
            ("stockquantity", "desc") => query.OrderByDescending(p => p.StockQuantity),
            ("stockquantity", _)      => query.OrderBy(p => p.StockQuantity),
            _                         => query.OrderBy(p => p.Name)
        };

        return await query.Skip(skip).Take(take)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                StockQuantity = p.StockQuantity
            })
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return null;
        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            ImageUrl = p.ImageUrl,
            Price = p.Price,
            StockQuantity = p.StockQuantity
        };
    }
}

public class EfCustomerQueryRepository : ICustomerQueryRepository
{
    private readonly RetailDbContext _db;
    public EfCustomerQueryRepository(RetailDbContext db) => _db = db;

    public Task<int> CountAsync(string? search = null)
    {
        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.FirstName.Contains(search) ||
                c.LastName.Contains(search) ||
                c.Email.Contains(search));

        return query.CountAsync();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return null;
        return new CustomerDto { Id = c.Id, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email };
    }

    public async Task<CustomerDto?> GetByEmailAsync(string email)
    {
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
        if (c == null) return null;
        return new CustomerDto { Id = c.Id, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email };
    }

    public async Task<List<CustomerDto>> GetPagedAsync(int skip, int take, string? sortBy, string? sortDirection, string? search = null)
    {
        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.FirstName.Contains(search) ||
                c.LastName.Contains(search) ||
                c.Email.Contains(search));

        query = (sortBy?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("firstname", "desc") => query.OrderByDescending(c => c.FirstName),
            ("firstname", _)      => query.OrderBy(c => c.FirstName),
            ("email", "desc")     => query.OrderByDescending(c => c.Email),
            ("email", _)          => query.OrderBy(c => c.Email),
            (_, "desc")           => query.OrderByDescending(c => c.LastName),
            _                     => query.OrderBy(c => c.LastName)
        };

        return await query.Skip(skip).Take(take)
            .Select(c => new CustomerDto { Id = c.Id, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email })
            .ToListAsync();
    }
}

public class EfOrderQueryRepository : IOrderQueryRepository
{
    private readonly RetailDbContext _db;
    public EfOrderQueryRepository(RetailDbContext db) => _db = db;

    public Task<int> CountAsync(OrderStatus? status, Guid? customerId, string? search = null)
    {
        var query = _db.Orders.Include(o => o.Customer).AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status);
        if (customerId.HasValue) query = query.Where(o => o.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o =>
                o.Customer.FirstName.Contains(search) ||
                o.Customer.LastName.Contains(search) ||
                o.Customer.Email.Contains(search));
        }
        return query.CountAsync();
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var o = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (o == null) return null;
        return MapOrder(o);
    }

    public async Task<List<OrderDto>> GetPagedAsync(int skip, int take, OrderStatus? status, string? sortBy, string? sortDirection, Guid? customerId, string? search = null)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (status.HasValue) query = query.Where(o => o.Status == status);
        if (customerId.HasValue) query = query.Where(o => o.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o =>
                o.Customer.FirstName.Contains(search) ||
                o.Customer.LastName.Contains(search) ||
                o.Customer.Email.Contains(search));
        }

        query = (sortBy?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("totalamount", "asc") => query.OrderBy(o => o.TotalAmount),
            ("totalamount", _) => query.OrderByDescending(o => o.TotalAmount),
            ("status", "asc") => query.OrderBy(o => o.Status),
            ("status", _) => query.OrderByDescending(o => o.Status),
            (_, "asc") => query.OrderBy(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        var orders = await query.Skip(skip).Take(take).ToListAsync();
        return orders.Select(MapOrder).ToList();
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(int limit)
    {
        return await _db.OrderItems
            .Where(oi => oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.SKU })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                SKU = g.Key.SKU,
                UnitsSold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<OrderSummaryDto> GetSummaryAsync()
    {
        var orders = await _db.Orders.ToListAsync();
        return new OrderSummaryDto
        {
            TotalOrders     = orders.Count,
            PendingOrders   = orders.Count(o => o.Status == OrderStatus.Pending),
            CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            TotalRevenue    = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount),
            PendingRevenue  = orders.Where(o => o.Status == OrderStatus.Pending).Sum(o => o.TotalAmount)
        };
    }

    private static OrderDto MapOrder(Order o) => new()
    {
        Id          = o.Id,
        CustomerId  = o.CustomerId,
        CustomerName = $"{o.Customer.FirstName} {o.Customer.LastName}",
        Status      = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        CreatedAt   = o.CreatedAt,
        Items       = o.OrderItems.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            Quantity  = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}
