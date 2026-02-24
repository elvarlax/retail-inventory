using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Exceptions;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;

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

            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
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
        }

        order.TotalAmount = total;

        await _orderRepository.AddAsync(order);
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
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException("Only pending orders can be completed.");

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;

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
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product != null)
                product.StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;

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
        string? sortDirection)
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

        var totalCount = await _orderRepository.CountAsync(parsedStatus);

        var orders = await _orderRepository.GetPagedAsync(
            skip,
            pageSize,
            parsedStatus,
            sortBy,
            sortDirection);

        return new PagedResultDto<OrderDto>
        {
            Items = _mapper.Map<List<OrderDto>>(orders),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task GenerateRandomOrdersAsync(int count)
    {
        var customers = await _customerRepository.GetAllAsync();
        var products = await _productRepository.GetAllAsync();

        if (!customers.Any() || !products.Any())
            return;

        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            var customer = customers[random.Next(customers.Count)];
            var product = products[random.Next(products.Count)];
            var quantity = random.Next(1, 4);

            try
            {
                var orderId = await CreateAsync(new CreateOrderRequest
                {
                    CustomerId = customer.Id,
                    Items =
                {
                    new CreateOrderItemRequest
                    {
                        ProductId = product.Id,
                        Quantity = quantity
                    }
                }
                });

                // Random status distribution
                var roll = random.Next(1, 101);

                if (roll <= 60)
                {
                    await CompleteAsync(orderId);
                }
                else if (roll <= 80)
                {
                    await CancelAsync(orderId);
                }
                // else: keep as Pending
            }
            catch
            {
                // ignore failures
            }
        }
    }
}