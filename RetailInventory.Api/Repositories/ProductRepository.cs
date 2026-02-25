using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly RetailDbContext _dbContext;

    public ProductRepository(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CountAsync()
    {
        return await _dbContext.Products.CountAsync();
    }

    public async Task<List<Product>> GetPagedAsync(
        int skip,
        int take,
        string? sortBy,
        string? sortDirection)
    {
        var query = _dbContext.Products.AsQueryable();
        var desc = sortDirection?.ToLower() == "desc";

        if (sortBy?.ToLower() == "price")
        {
            query = desc
                ? query.OrderByDescending(p => p.Price)
                : query.OrderBy(p => p.Price);
        }
        else if (sortBy?.ToLower() == "sku")
        {
            query = desc
                ? query.OrderByDescending(p => p.SKU)
                : query.OrderBy(p => p.SKU);
        }
        else if (sortBy?.ToLower() == "stockquantity")
        {
            query = desc
                ? query.OrderByDescending(p => p.StockQuantity)
                : query.OrderBy(p => p.StockQuantity);
        }
        else
        {
            query = desc
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name);
        }

        return await query
            .AsNoTracking()
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await _dbContext.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _dbContext.Products.AddAsync(product);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}