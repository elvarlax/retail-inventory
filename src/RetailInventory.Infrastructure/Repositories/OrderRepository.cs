using Microsoft.EntityFrameworkCore;
using RetailInventory.Application.Interfaces;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Data;

namespace RetailInventory.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly RetailDbContext _dbContext;

    public OrderRepository(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Order order)
    {
        await _dbContext.Orders.AddAsync(order);
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetOrderForUpdateAsync(Guid id)
    {
        return await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public Task DeleteAsync(Order order)
    {
        _dbContext.Orders.Remove(order);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
