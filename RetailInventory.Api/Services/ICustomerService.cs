using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Services;

public interface ICustomerService
{
    Task<Customer> CreateAsync(RegisterRequestDto request);
    Task<CustomerDto?> GetByIdAsync(Guid id);
    Task<PagedResultDto<CustomerDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection);
}
