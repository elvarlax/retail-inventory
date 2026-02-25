using Bogus;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Data;

public class ProductGenerator
{
    private readonly RetailDbContext _dbContext;

    private static readonly string[] Categories =
        ["ELEC", "CLTH", "HOME", "SPRT", "FOOD", "BOOK", "TOYS", "AUTO"];

    public ProductGenerator(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GenerateAsync(int count)
    {
        var faker = new Faker<Product>()
            .RuleFor(p => p.Id, _ => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.SKU, f =>
            {
                var category = f.PickRandom(Categories);
                var suffix = Guid.NewGuid().ToString("N")[..8].ToUpper();
                return $"{category}-{suffix}";
            })
            .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(1m, 500m), 2))
            .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 500));

        const int batchSize = 5000;
        var generated = 0;

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            while (generated < count)
            {
                var take = Math.Min(batchSize, count - generated);
                var batch = faker.Generate(take);

                _dbContext.Products.AddRange(batch);
                await _dbContext.SaveChangesAsync();
                _dbContext.ChangeTracker.Clear();

                generated += take;
            }
        }
        finally
        {
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return generated;
    }
}
