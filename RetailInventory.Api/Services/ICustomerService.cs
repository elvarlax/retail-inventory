using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Services;

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(Guid id);
    Task<PagedResultDto<CustomerDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection);
}
