using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
}