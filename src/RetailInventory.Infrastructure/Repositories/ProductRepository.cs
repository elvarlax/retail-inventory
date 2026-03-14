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

    public async Task AddAsync(Product product)
    {
        await _dbContext.Products.AddAsync(product);
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _dbContext.Products.FirstOrDefaultAsync(p => p.SKU == sku);
    }

    public async Task<Product?> GetByNameAsync(string name)
    {
        return await _dbContext.Products.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _dbContext.Products
            .Where(p => idList.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Products.FindAsync(id);
    }

    public Task DeleteAsync(Product product)
    {
        _dbContext.Products.Remove(product);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
