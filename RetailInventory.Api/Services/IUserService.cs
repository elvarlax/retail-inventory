using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Services;

public interface IUserService
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(RegisterRequestDto request);
}
