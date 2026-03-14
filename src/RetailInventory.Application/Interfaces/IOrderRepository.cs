using RetailInventory.Domain;

namespace RetailInventory.Application.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetOrderForUpdateAsync(Guid id);
    Task DeleteAsync(Order order);
    Task SaveChangesAsync();
}
