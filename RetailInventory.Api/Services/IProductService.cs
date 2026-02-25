using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Services;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<PagedResultDto<ProductDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection);
}
