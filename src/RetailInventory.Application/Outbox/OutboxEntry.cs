namespace RetailInventory.Application.Outbox;

public record OutboxEntry(
    Guid Id,
    string Type,
    string Source,
    string Payload,
    DateTime OccurredAtUtc
);
