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

    public async Task<Order?> GetOrderForUpdateAsync(Guid id)
    {
        return await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<int> CountAsync(OrderStatus? status)
    {
        var query = _dbContext.Orders.AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        return await query.CountAsync();
    }

    public async Task<List<Order>> GetPagedAsync(
        int skip,
        int take,
        OrderStatus? status,
        string? sortBy,
        string? sortDirection)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        var desc = sortDirection?.ToLower() == "desc";

        if (sortBy?.ToLower() == "totalamount")
        {
            query = desc
                ? query.OrderByDescending(o => o.TotalAmount)
                : query.OrderBy(o => o.TotalAmount);
        }
        else if (sortBy?.ToLower() == "status")
        {
            query = desc
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status);
        }
        else // default CreatedAt
        {
            query = desc
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt);
        }

        return await query
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
        var grouped = await _dbContext.Orders
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync();

        var byStatus = grouped.ToDictionary(g => g.Status);

        var pending   = byStatus.GetValueOrDefault(OrderStatus.Pending);
        var completed = byStatus.GetValueOrDefault(OrderStatus.Completed);
        var cancelled = byStatus.GetValueOrDefault(OrderStatus.Cancelled);

        return new OrderSummaryDto
        {
            TotalOrders     = grouped.Sum(g => g.Count),
            PendingOrders   = pending?.Count     ?? 0,
            CompletedOrders = completed?.Count   ?? 0,
            CancelledOrders = cancelled?.Count   ?? 0,
            TotalRevenue    = completed?.Revenue ?? 0m,
            PendingRevenue  = pending?.Revenue   ?? 0m
        };
    }
}