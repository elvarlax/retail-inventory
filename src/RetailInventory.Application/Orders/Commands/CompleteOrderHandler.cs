using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.Events;
using RetailInventory.Application.Outbox;
using RetailInventory.Domain;
using System.Text.Json;

namespace RetailInventory.Application.Orders.Commands;

public class CompleteOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxRepository _outboxRepository;

    public CompleteOrderHandler(IOrderRepository orderRepository, IOutboxRepository outboxRepository)
    {
        _orderRepository = orderRepository;
        _outboxRepository = outboxRepository;
    }

    public async Task Handle(CompleteOrderCommand command)
    {
        var order = await _orderRepository.GetOrderForUpdateAsync(command.OrderId)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException("Only pending orders can be completed.");

        var occurredAt = DateTime.UtcNow;

        order.Status = OrderStatus.Completed;
        order.CompletedAt = occurredAt;

        var @event = new OrderStatusChangedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            OrderId = order.Id,
            PreviousStatus = nameof(OrderStatus.Pending),
            NewStatus = nameof(OrderStatus.Completed)
        };

        await _outboxRepository.AddAsync(new OutboxEntry(
            Id: @event.EventId,
            Type: nameof(OrderStatusChangedV1),
            Source: OutboxConstants.Source,
            Payload: JsonSerializer.Serialize(@event),
            OccurredAtUtc: occurredAt
        ));

        await _orderRepository.SaveChangesAsync();
    }
}
