using RetailInventory.Domain;

namespace RetailInventory.Application.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsAsync(string email);
    Task DeleteAsync(User user);
    Task SaveChangesAsync();
}
