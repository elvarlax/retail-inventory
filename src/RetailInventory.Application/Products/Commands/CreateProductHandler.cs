using AutoMapper;
using MediatR;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Outbox;
using RetailInventory.Application.Products.Events;
using RetailInventory.Domain;
using System.Text.Json;

namespace RetailInventory.Application.Products.Commands;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public CreateProductHandler(IProductRepository productRepository, IOutboxRepository outboxRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateProductCommand command, CancellationToken ct)
    {
        if (await _productRepository.GetBySkuAsync(command.SKU, ct) != null)
            throw new ConflictException($"SKU '{command.SKU}' is already in use.");

        if (await _productRepository.GetByNameAsync(command.Name, ct) != null)
            throw new ConflictException($"A product named '{command.Name}' already exists.");

        var product = _mapper.Map<Product>(command);
        product.Id = Guid.NewGuid();

        await _productRepository.AddAsync(product, ct);

        var occurredAt = DateTime.UtcNow;

        var @event = new ProductCreatedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            ProductId = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        };

        await _outboxRepository.AddAsync(new OutboxEntry(
            Id: @event.EventId,
            Type: nameof(ProductCreatedV1),
            Source: OutboxConstants.Source,
            Payload: JsonSerializer.Serialize(@event),
            OccurredAtUtc: occurredAt
        ));

        await _productRepository.SaveChangesAsync(ct);

        return product.Id;
    }
}
