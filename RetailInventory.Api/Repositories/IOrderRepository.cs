using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task AddAsync(Order order);
    Task<List<Order>> GetPagedAsync(int skip, int take, OrderStatus? status);
    Task<int> CountAsync(OrderStatus? status);
    Task SaveChangesAsync();
}