using RetailInventory.Domain;

namespace RetailInventory.Application.Interfaces;

public interface ICustomerRepository
{
    Task AddAsync(Customer customer);
    Task<bool> ExistsAsync(Guid id);
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByEmailAsync(string email);
    Task DeleteAsync(Customer customer);
    Task SaveChangesAsync();
}
