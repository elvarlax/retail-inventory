using RetailInventory.Application.Outbox;

namespace RetailInventory.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEntry entry);
}
