using MediatR;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Products.DTOs;

namespace RetailInventory.Application.Products.Queries;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductQueryRepository _queryRepository;

    public GetProductByIdHandler(IProductQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery query, CancellationToken ct)
    {
        return await _queryRepository.GetByIdAsync(query.Id);
    }
}
