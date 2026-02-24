using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Services;

public interface IProductService
{
    Task<int> ImportFromExternalAsync();
    Task<List<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<PagedResultDto<ProductDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection);
}