using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Services
{
    public interface IProductService
    {
        Task<int> ImportFromExternalAsync();
        Task<List<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(Guid id);
    }
}
