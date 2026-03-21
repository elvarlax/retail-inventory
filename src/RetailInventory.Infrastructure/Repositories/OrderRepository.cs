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

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _dbContext.Orders.AddAsync(order, ct);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Order?> GetOrderForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public Task DeleteAsync(Order order, CancellationToken ct = default)
    {
        _dbContext.Orders.Remove(order);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
