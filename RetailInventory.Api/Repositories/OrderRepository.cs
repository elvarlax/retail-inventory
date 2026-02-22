using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RetailInventory.Api.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;

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

    public async Task<int> CountAsync(OrderStatus? status)
    {
        var query = _dbContext.Orders.AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        return await query.CountAsync();
    }

    public async Task<List<Order>> GetPagedAsync(int skip, int take, OrderStatus? status)
    {
        var query = _dbContext.Orders
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task<OrderSummaryDto> GetSummaryAsync()
    {
        var totalOrders = await _dbContext.Orders.CountAsync();
        var pendingOrders = await _dbContext.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var completedOrders = await _dbContext.Orders.CountAsync(o => o.Status == OrderStatus.Completed);
        var cancelledOrders = await _dbContext.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);

        var totalRevenue = await _dbContext.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount);

        var pendingRevenue = await _dbContext.Orders
            .Where(o => o.Status == OrderStatus.Pending)
            .SumAsync(o => o.TotalAmount);

        return new OrderSummaryDto
        {
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            CompletedOrders = completedOrders,
            CancelledOrders = cancelledOrders,
            TotalRevenue = totalRevenue,
            PendingRevenue = pendingRevenue
        };
    }
}