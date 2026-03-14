using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Outbox;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Data;

namespace RetailInventory.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly RetailDbContext _dbContext;

    public OutboxRepository(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(OutboxEntry entry)
    {
        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = entry.Id,
            Type = entry.Type,
            Source = entry.Source,
            Payload = entry.Payload,
            OccurredAtUtc = entry.OccurredAtUtc
        });

        return Task.CompletedTask;
    }
}
