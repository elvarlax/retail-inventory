using RetailInventory.Api.DTOs;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;

namespace RetailInventory.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly IDummyJsonService _dummyService;
        private readonly IProductRepository _repository;

        public ProductService(
            IDummyJsonService dummyService,
            IProductRepository repository)
        {
            _dummyService = dummyService;
            _repository = repository;
        }

        public async Task<List<ProductDto>> GetAllAsync()
        {
            var products = await _repository.GetAllAsync();

            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                StockQuantity = p.StockQuantity,
                Price = p.Price
            }).ToList();
        }

        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync(id);

            if (product == null)
                return null;

            return new ProductDto 
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                StockQuantity = product.StockQuantity,
                Price = product.Price
            };
        }

        public async Task<int> ImportFromExternalAsync()
        {
            var externalProducts = await _dummyService.GetProductsAsync();
            var inserted = 0;

            foreach (var item in externalProducts)
            {
                var exists = await _repository.ExistsByExternalIdAsync(item.Id);

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

                await _repository.AddAsync(product);
                inserted++;
            }

            await _repository.SaveChangesAsync();
            return inserted;
        }
    }
}
