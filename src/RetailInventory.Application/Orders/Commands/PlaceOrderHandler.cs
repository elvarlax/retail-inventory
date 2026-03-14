using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Orders.Events;
using RetailInventory.Application.Outbox;
using RetailInventory.Domain;
using System.Text.Json;

namespace RetailInventory.Application.Orders.Commands;

public class PlaceOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOutboxRepository _outboxRepository;

    public PlaceOrderHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IOutboxRepository outboxRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _outboxRepository = outboxRepository;
    }

    public async Task<Guid> Handle(PlaceOrderCommand command)
    {
        if (command.CustomerId == Guid.Empty)
            throw new BadRequestException("CustomerId is required.");

        if (command.Items == null || command.Items.Count == 0)
            throw new BadRequestException("Order must contain at least one item.");

        if (command.Items.Any(i => i.Quantity <= 0))
            throw new BadRequestException("Quantity must be greater than zero.");

        if (!await _customerRepository.ExistsAsync(command.CustomerId))
            throw new NotFoundException("Customer not found.");

        var requestedIds = command.Items.Select(i => i.ProductId).Distinct();
        var products = await _productRepository.GetByIdsAsync(requestedIds);
        var productMap = products.ToDictionary(p => p.Id);

        var occurredAt = DateTime.UtcNow;
        decimal total = 0m;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = command.CustomerId,
            Status = OrderStatus.Pending,
            CreatedAt = occurredAt
        };

        var itemSnapshots = new List<OrderItemSnapshot>();

        foreach (var item in command.Items)
        {
            if (item.Quantity <= 0)
                throw new BadRequestException("Quantity must be greater than zero.");

            if (!productMap.TryGetValue(item.ProductId, out var product))
                throw new NotFoundException($"Product {item.ProductId} not found.");

            if (product.StockQuantity < item.Quantity)
                throw new BadRequestException($"Insufficient stock for product {product.Name}.");

            product.StockQuantity -= item.Quantity;

            order.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });

            total += product.Price * item.Quantity;

            itemSnapshots.Add(new OrderItemSnapshot
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        order.TotalAmount = total;

        await _orderRepository.AddAsync(order);

        var @event = new OrderPlacedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Items = itemSnapshots
        };

        await _outboxRepository.AddAsync(new OutboxEntry(
            Id: @event.EventId,
            Type: nameof(OrderPlacedV1),
            Source: OutboxConstants.Source,
            Payload: JsonSerializer.Serialize(@event),
            OccurredAtUtc: occurredAt
        ));

        // SaveChangesAsync commits the new order, stock decrements, and outbox entry atomically.
        await _orderRepository.SaveChangesAsync();

        return order.Id;
    }
}
