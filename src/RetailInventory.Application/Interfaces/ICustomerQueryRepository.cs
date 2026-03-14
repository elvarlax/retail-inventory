using RetailInventory.Application.Customers.DTOs;

namespace RetailInventory.Application.Interfaces;

public interface ICustomerQueryRepository
{
    Task<int> CountAsync(string? search = null);
    Task<CustomerDto?> GetByIdAsync(Guid id);
    Task<CustomerDto?> GetByEmailAsync(string email);
    Task<List<CustomerDto>> GetPagedAsync(int skip, int take, string? sortBy, string? sortDirection, string? search = null);
}
