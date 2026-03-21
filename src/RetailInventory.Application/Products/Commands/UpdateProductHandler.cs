using MediatR;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Products.Commands;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IProductRepository _repository;

    public UpdateProductHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateProductCommand command, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Product not found.");

        // Check name conflict (excluding self)
        var nameConflict = await _repository.GetByNameAsync(command.Name, ct);
        if (nameConflict != null && nameConflict.Id != command.Id)
            throw new ConflictException($"A product named '{command.Name}' already exists.");

        // Check SKU conflict (excluding self)
        var skuConflict = await _repository.GetBySkuAsync(command.SKU, ct);
        if (skuConflict != null && skuConflict.Id != command.Id)
            throw new ConflictException($"SKU '{command.SKU}' is already in use.");

        product.Name = command.Name;
        product.SKU = command.SKU;
        product.ImageUrl = command.ImageUrl;
        product.Price = command.Price;
        product.StockQuantity = command.StockQuantity;

        await _repository.SaveChangesAsync(ct);
    }
}
