using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

public interface IProductRepository
{
    Task<bool> ExistsByExternalIdAsync(int externalId);
    Task<int> CountAsync();
    Task<List<Product>> GetAllAsync();
    Task<List<Product>> GetPagedAsync(
        int skip,
        int take,
        string? sortBy,
        string? sortDirection);
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task SaveChangesAsync();
}