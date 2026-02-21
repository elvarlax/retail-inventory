using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly RetailDbContext _dbContext;

        public OrderRepository(RetailDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Order?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task AddAsync(Order order)
        {
            await _dbContext.Orders.AddAsync(order);
        }

        public async Task<List<Order>> GetPagedAsync(int skip, int take, OrderStatus? status)
        {
            var query = _dbContext.Orders
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountAsync(OrderStatus? status)
        {
            var query = _dbContext.Orders.AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            return await query.CountAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
