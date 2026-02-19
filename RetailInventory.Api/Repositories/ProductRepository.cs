using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly RetailDbContext _dbContext;

        public ProductRepository(RetailDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ExistsByExternalIdAsync(int externalId)
        {
            return await _dbContext.Products.AnyAsync(p => p.ExternalId == externalId);
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _dbContext.Products.AsNoTracking().ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
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
}
