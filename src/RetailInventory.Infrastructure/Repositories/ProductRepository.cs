using Microsoft.EntityFrameworkCore;
using RetailInventory.Application.Interfaces;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Data;

namespace RetailInventory.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly RetailDbContext _dbContext;

    public ProductRepository(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _dbContext.Products.AddAsync(product, ct);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        return await _dbContext.Products.FirstOrDefaultAsync(p => p.SKU == sku, ct);
    }

    public async Task<Product?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _dbContext.Products.FirstOrDefaultAsync(p => p.Name == name, ct);
    }

    public async Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await _dbContext.Products
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(ct);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Products.FindAsync([id], ct);
    }

    public Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        _dbContext.Products.Remove(product);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
