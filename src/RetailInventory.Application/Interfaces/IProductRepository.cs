using RetailInventory.Domain;

namespace RetailInventory.Application.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product);
    Task<Product?> GetBySkuAsync(string sku);
    Task<Product?> GetByNameAsync(string name);
    Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<Product?> GetByIdAsync(Guid id);
    Task DeleteAsync(Product product);
    Task SaveChangesAsync();
}
