using RetailInventory.Api.Models;

namespace RetailInventory.Api.Services;

public interface ITokenService
{
    string CreateToken(User user);
}