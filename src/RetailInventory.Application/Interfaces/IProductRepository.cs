using RetailInventory.Domain;

namespace RetailInventory.Application.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken ct = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<Product?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Product product, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
