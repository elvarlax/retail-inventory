using Bogus;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Data;

public class OrderGenerator
{
    private readonly RetailDbContext _dbContext;

    public OrderGenerator(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GenerateAsync(int count)
    {
        var customerIds = await _dbContext.Customers
            .AsNoTracking()
            .Select(c => c.Id)
            .ToListAsync();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Select(p => new { p.Id, p.Price })
            .ToListAsync();

        if (customerIds.Count == 0 || products.Count == 0)
            throw new InvalidOperationException(
                "Customers and products must exist before generating orders.");

        var now = DateTime.UtcNow;
        var twelveMonthsAgo = now.AddMonths(-12);
        var faker = new Faker();

        const int batchSize = 5000;
        var generated = 0;

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            while (generated < count)
            {
                var take = Math.Min(batchSize, count - generated);
                var batch = new List<Order>(take);

                for (int i = 0; i < take; i++)
                {
                    var createdAt = faker.Date.Between(twelveMonthsAgo, now);
                    var status = PickStatus(faker);

                    var itemCount = Math.Min(faker.Random.Int(1, 4), products.Count);
                    var selectedProducts = faker.PickRandom(products, itemCount).ToList();

                    var orderItems = selectedProducts.Select(p => new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = p.Id,
                        Quantity = faker.Random.Int(1, 5),
                        UnitPrice = p.Price
                    }).ToList();

                    var orderId = Guid.NewGuid();

                    foreach (var item in orderItems)
                        item.OrderId = orderId;

                    batch.Add(new Order
                    {
                        Id = orderId,
                        CustomerId = faker.PickRandom(customerIds),
                        Status = status,
                        CreatedAt = createdAt,
                        CompletedAt = status == OrderStatus.Completed
                            ? createdAt.AddHours(faker.Random.Double(1, 72))
                            : null,
                        TotalAmount = orderItems.Sum(oi => oi.Quantity * oi.UnitPrice),
                        OrderItems = orderItems
                    });
                }

                _dbContext.Orders.AddRange(batch);
                await _dbContext.SaveChangesAsync();
                _dbContext.ChangeTracker.Clear();

                generated += take;
            }
        }
        finally
        {
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return generated;
    }

    private static OrderStatus PickStatus(Faker faker)
    {
        var roll = faker.Random.Int(1, 100);
        return roll switch
        {
            <= 60 => OrderStatus.Completed,
            <= 80 => OrderStatus.Cancelled,
            _ => OrderStatus.Pending
        };
    }
}
