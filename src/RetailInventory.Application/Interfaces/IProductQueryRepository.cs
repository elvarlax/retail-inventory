using RetailInventory.Application.Products.DTOs;

namespace RetailInventory.Application.Interfaces;

public interface IProductQueryRepository
{
    Task<int> CountAsync(string? search = null);
    Task<List<ProductDto>> GetPagedAsync(int skip, int take, string? sortBy, string? sortDirection, string? search = null);
    Task<ProductDto?> GetByIdAsync(Guid id);
}
