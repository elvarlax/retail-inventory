using RetailInventory.Domain;

namespace RetailInventory.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(User user, Guid? customerId = null);
}
