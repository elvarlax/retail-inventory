using MediatR;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Products.Commands;

public class RestockProductHandler : IRequestHandler<RestockProductCommand>
{
    private readonly IProductRepository _repository;

    public RestockProductHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RestockProductCommand command, CancellationToken ct)
    {
        if (command.Quantity <= 0)
            throw new BadRequestException("Restock quantity must be greater than zero.");

        var product = await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Product not found.");

        product.StockQuantity += command.Quantity;
        await _repository.SaveChangesAsync(ct);
    }
}
