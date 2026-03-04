using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Events;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;
using System.Text.Json;

namespace RetailInventory.Api.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<Guid> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            SKU = request.SKU,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        await _productRepository.AddAsync(product);

        var occurredAt = DateTime.UtcNow;

        var productCreatedEvent = new ProductCreatedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            ProductId = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        };

        await _productRepository.AddOutboxMessageAsync(new OutboxMessage
        {
            Id = productCreatedEvent.EventId,
            Type = nameof(ProductCreatedV1),
            Source = OutboxConstants.Source,
            Payload = JsonSerializer.Serialize(productCreatedEvent),
            OccurredAtUtc = occurredAt
        });

        await _productRepository.SaveChangesAsync();

        return product.Id;
    }

    public async Task<PagedResultDto<ProductDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || pageSize > 50) pageSize = 10;

        var skip = (pageNumber - 1) * pageSize;

        var totalCount = await _productRepository.CountAsync();
        var products = await _productRepository.GetPagedAsync(skip, pageSize, sortBy, sortDirection);

        return new PagedResultDto<ProductDto>
        {
            Items = _mapper.Map<List<ProductDto>>(products),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            return null;

        return _mapper.Map<ProductDto>(product);
    }
}
