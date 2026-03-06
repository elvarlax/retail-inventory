using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Events;
using RetailInventory.Api.Exceptions;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using System.Text.Json;

namespace RetailInventory.Api.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<Guid> CreateAsync(CreateOrderRequest request)
    {
        if (request.CustomerId == Guid.Empty)
            throw new BadRequestException("CustomerId is required.");

        if (request.Items == null || !request.Items.Any())
            throw new BadRequestException("Order must contain at least one item.");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
            throw new NotFoundException("Customer not found.");

        using var transaction = await _orderRepository.BeginTransactionAsync();

        var requestedIds = request.Items.Select(i => i.ProductId).Distinct();
        var products = await _productRepository.GetByIdsAsync(requestedIds);
        var productMap = products.ToDictionary(p => p.Id);

        var occurredAt = DateTime.UtcNow;
        decimal total = 0m;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            CreatedAt = occurredAt
        };

        var itemSnapshots = new List<OrderItemSnapshot>();

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                throw new BadRequestException("Quantity must be greater than zero.");

            if (!productMap.TryGetValue(item.ProductId, out var product))
                throw new NotFoundException("Product not found.");

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

        var orderPlacedEvent = new OrderPlacedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Items = itemSnapshots
        };

        await _orderRepository.AddOutboxMessageAsync(new OutboxMessage
        {
            Id = orderPlacedEvent.EventId,
            Type = nameof(OrderPlacedV1),
            Source = OutboxConstants.Source,
            Payload = JsonSerializer.Serialize(orderPlacedEvent),
            OccurredAtUtc = occurredAt
        });

        await _orderRepository.SaveChangesAsync();
        await transaction.CommitAsync();

        return order.Id;
    }

    public async Task<OrderDto> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            throw new NotFoundException("Order not found.");

        return _mapper.Map<OrderDto>(order);
    }

    public async Task CompleteAsync(Guid id)
    {
        var order = await _orderRepository.GetOrderForUpdateAsync(id);

        if (order == null)
            throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException("Only pending orders can be completed.");

        var occurredAt = DateTime.UtcNow;

        order.Status = OrderStatus.Completed;
        order.CompletedAt = occurredAt;

        var statusEvent = new OrderStatusChangedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            OrderId = order.Id,
            PreviousStatus = nameof(OrderStatus.Pending),
            NewStatus = nameof(OrderStatus.Completed)
        };

        await _orderRepository.AddOutboxMessageAsync(new OutboxMessage
        {
            Id = statusEvent.EventId,
            Type = nameof(OrderStatusChangedV1),
            Source = OutboxConstants.Source,
            Payload = JsonSerializer.Serialize(statusEvent),
            OccurredAtUtc = occurredAt
        });

        await _orderRepository.SaveChangesAsync();
    }

    public async Task CancelAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException("Only pending orders can be cancelled.");

        var productIds = order.OrderItems.Select(i => i.ProductId).Distinct();
        var products = await _productRepository.GetByIdsAsync(productIds);
        var productMap = products.ToDictionary(p => p.Id);

        foreach (var item in order.OrderItems)
        {
            if (productMap.TryGetValue(item.ProductId, out var product))
                product.StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;

        var occurredAt = DateTime.UtcNow;

        var statusEvent = new OrderStatusChangedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            OrderId = order.Id,
            PreviousStatus = nameof(OrderStatus.Pending),
            NewStatus = nameof(OrderStatus.Cancelled)
        };

        await _orderRepository.AddOutboxMessageAsync(new OutboxMessage
        {
            Id = statusEvent.EventId,
            Type = nameof(OrderStatusChangedV1),
            Source = OutboxConstants.Source,
            Payload = JsonSerializer.Serialize(statusEvent),
            OccurredAtUtc = occurredAt
        });

        await _orderRepository.SaveChangesAsync();
    }

    public async Task<OrderSummaryDto> GetSummaryAsync()
    {
        return await _orderRepository.GetSummaryAsync();
    }

    public async Task<PagedResultDto<OrderDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        string? sortBy,
        string? sortDirection,
        Guid? customerId = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || pageSize > 50) pageSize = 10;

        OrderStatus? parsedStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var s))
                throw new BadRequestException("Invalid order status filter.");

            parsedStatus = s;
        }

        var skip = (pageNumber - 1) * pageSize;

        var totalCount = await _orderRepository.CountAsync(parsedStatus, customerId);

        var orders = await _orderRepository.GetPagedAsync(
            skip,
            pageSize,
            parsedStatus,
            sortBy,
            sortDirection,
            customerId);

        return new PagedResultDto<OrderDto>
        {
            Items = _mapper.Map<List<OrderDto>>(orders),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
