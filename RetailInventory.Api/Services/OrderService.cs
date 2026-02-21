using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Exceptions;
using RetailInventory.Api.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;
using AutoMapper;
using RetailInventory.Api.Repositories;

namespace RetailInventory.Api.Services;

public class OrderService : IOrderService
{
    private readonly RetailDbContext _dbContext;
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public OrderService(RetailDbContext dbContext, IOrderRepository orderRepository, IMapper mapper)
    {
        _dbContext = dbContext;
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<Guid> CreateAsync(CreateOrderRequest request)
    {
        // Basic request validation
        if (request.CustomerId == Guid.Empty)
            throw new BadRequestException("CustomerId is required.");

        if (request.Items == null || !request.Items.Any())
            throw new BadRequestException("Order must contain at least one item.");

        // Ensure customer exists
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId);

        if (customer == null)
            throw new NotFoundException("Customer not found.");

        // Begin transaction to ensure atomic order creation
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        decimal total = 0m;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                throw new BadRequestException("Quantity must be greater than zero.");

            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product == null)
                throw new NotFoundException("Product not found.");

            // Ensure sufficient stock
            if (product.StockQuantity < item.Quantity)
                throw new BadRequestException($"Insufficient stock for product {product.Name}.");

            // Deduct stock
            product.StockQuantity -= item.Quantity;

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };

            total += product.Price * item.Quantity;

            order.OrderItems.Add(orderItem);
        }

        order.TotalAmount = total;

        await _orderRepository.AddAsync(order);

        await _orderRepository.SaveChangesAsync();

        // Commit transaction only after all changes succeed
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
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException("Only pending orders can be completed.");

        order.Status = OrderStatus.Completed;

        await _orderRepository.SaveChangesAsync();
    }

    public async Task CancelAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException("Only pending orders can be cancelled.");

        foreach (var item in order.OrderItems)
        {
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product != null)
            {
                // Restock the product
                product.StockQuantity += item.Quantity;
            }
        }

        order.Status = OrderStatus.Cancelled;

        await _orderRepository.SaveChangesAsync();
    }

    public async Task<OrderSummaryDto> GetSummaryAsync()
    {
        var totalOrders = await _dbContext.Orders
            .CountAsync();
        
        var pendingOrders = await _dbContext.Orders
            .CountAsync(o => o.Status == OrderStatus.Pending);
        var completedOrders = await _dbContext.Orders
            .CountAsync(o => o.Status == OrderStatus.Completed);
        var cancelledOrders = await _dbContext.Orders
            .CountAsync(o => o.Status == OrderStatus.Cancelled);

        var totalRevenue = await _dbContext.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount);
        var pendingRevenue = await _dbContext.Orders
            .Where(o => o.Status == OrderStatus.Pending)
            .SumAsync(o => o.TotalAmount);

        return new OrderSummaryDto
        {
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            CompletedOrders = completedOrders,
            CancelledOrders = cancelledOrders,
            TotalRevenue = totalRevenue,
            PendingRevenue = pendingRevenue
        };
    }

    public async Task<PagedResultDto<OrderDto>> GetPagedAsync(int pageNumber, int pageSize, string? status)
    {
        if (pageNumber <= 0)
            pageNumber = 1;

        if (pageSize <= 0 || pageSize > 50)
            pageSize = 10;

        OrderStatus? parsedStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var s))
                throw new BadRequestException("Invalid order status filter.");

            parsedStatus = s;
        }

        var skip = (pageNumber - 1) * pageSize;

        var totalCount = await _orderRepository
            .CountAsync(parsedStatus);

        var orders = await _orderRepository
            .GetPagedAsync(skip, pageSize, parsedStatus);

        var items = _mapper.Map<List<OrderDto>>(orders);

        return new PagedResultDto<OrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}