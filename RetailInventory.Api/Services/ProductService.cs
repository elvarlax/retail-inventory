using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Repositories;

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
