using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

public interface ICustomerRepository
{
    Task<bool> ExistsByExternalIdAsync(int externalId);
    Task AddAsync(Customer customer);
    Task SaveChangesAsync();
}