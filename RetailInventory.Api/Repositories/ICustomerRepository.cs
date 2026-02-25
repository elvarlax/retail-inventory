using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task AddAsync(Customer customer);
    Task SaveChangesAsync();
    Task<int> CountAsync();
    Task<List<Customer>> GetPagedAsync(
        int skip,
        int take,
        string? sortBy,
        string? sortDirection);
}