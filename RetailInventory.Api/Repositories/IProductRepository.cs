using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

public interface IProductRepository
{
    Task<bool> ExistsByExternalIdAsync(int externalId);
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task SaveChangesAsync();
}