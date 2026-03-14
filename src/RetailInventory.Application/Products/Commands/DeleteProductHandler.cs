using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Products.Commands;

public class DeleteProductHandler
{
    private readonly IProductRepository _repository;

    public DeleteProductHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteProductCommand command)
    {
        var product = await _repository.GetByIdAsync(command.Id)
            ?? throw new NotFoundException("Product not found.");

        await _repository.DeleteAsync(product);
        await _repository.SaveChangesAsync();
    }
}
