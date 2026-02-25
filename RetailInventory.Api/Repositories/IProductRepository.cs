using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

public interface IProductRepository
{
    Task<int> CountAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task AddAsync(Product product);
    Task SaveChangesAsync();
    Task<List<Product>> GetPagedAsync(
        int skip,
        int take,
        string? sortBy,
        string? sortDirection);
}