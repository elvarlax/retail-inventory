using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Products.DTOs;

namespace RetailInventory.Application.Products.Queries;

public class GetProductByIdHandler
{
    private readonly IProductQueryRepository _queryRepository;

    public GetProductByIdHandler(IProductQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery query)
    {
        return await _queryRepository.GetByIdAsync(query.Id);
    }
}
