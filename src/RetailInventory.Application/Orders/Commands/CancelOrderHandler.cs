using MediatR;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.Events;
using RetailInventory.Application.Outbox;
using RetailInventory.Domain;
using System.Text.Json;

namespace RetailInventory.Application.Orders.Commands;

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOutboxRepository _outboxRepository;

    public CancelOrderHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IOutboxRepository outboxRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _outboxRepository = outboxRepository;
    }

    public async Task Handle(CancelOrderCommand command, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, ct)
            ?? throw new NotFoundException("Order not found.");

        if (command.RequestingCustomerId.HasValue && order.CustomerId != command.RequestingCustomerId.Value)
            throw new ForbiddenException("You can only cancel your own orders.");

        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException("Only pending orders can be cancelled.");

        var productIds = order.OrderItems.Select(i => i.ProductId).Distinct();
        var products = await _productRepository.GetByIdsAsync(productIds, ct);
        var productMap = products.ToDictionary(p => p.Id);

        foreach (var item in order.OrderItems)
        {
            if (productMap.TryGetValue(item.ProductId, out var product))
                product.StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;

        var occurredAt = DateTime.UtcNow;

        var @event = new OrderStatusChangedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            OrderId = order.Id,
            PreviousStatus = nameof(OrderStatus.Pending),
            NewStatus = nameof(OrderStatus.Cancelled)
        };

        await _outboxRepository.AddAsync(new OutboxEntry(
            Id: @event.EventId,
            Type: nameof(OrderStatusChangedV1),
            Source: OutboxConstants.Source,
            Payload: JsonSerializer.Serialize(@event),
            OccurredAtUtc: occurredAt
        ));

        await _orderRepository.SaveChangesAsync(ct);
    }
}
