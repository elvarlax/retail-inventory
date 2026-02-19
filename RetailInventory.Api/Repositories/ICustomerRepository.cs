using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories
{
    public interface ICustomerRepository
    {
        Task<bool> ExistsByExternalIdAsync(int externalId);
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(Guid id);
        Task AddAsync(Customer customer);
        Task SaveChangesAsync();
    }
}