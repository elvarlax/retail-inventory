using AutoMapper;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;

namespace RetailInventory.Api.Services;

public class ProductService : IProductService
{
    private readonly IDummyJsonService _dummyService;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public ProductService(
        IDummyJsonService dummyService,
        IProductRepository productRepository,
        IMapper mapper)
    {
        _dummyService = dummyService;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<int> ImportFromExternalAsync()
    {
        var externalProducts = await _dummyService.GetProductsAsync();
        var inserted = 0;

        foreach (var item in externalProducts)
        {
            var exists = await _productRepository.ExistsByExternalIdAsync(item.Id);

            if (exists)
                continue;

            var product = new Product
            {
                Id = Guid.NewGuid(),
                ExternalId = item.Id,
                Name = item.Title,
                SKU = $"DUMMY-{item.Id}",
                StockQuantity = item.Stock,
                Price = item.Price
            };

            await _productRepository.AddAsync(product);
            inserted++;
        }

        await _productRepository.SaveChangesAsync();
        return inserted;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return _mapper.Map<List<ProductDto>>(products);
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