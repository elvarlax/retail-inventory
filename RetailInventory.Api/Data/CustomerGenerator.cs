using Bogus;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Data;

public class CustomerGenerator
{
    private readonly RetailDbContext _dbContext;

    public CustomerGenerator(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GenerateAsync(int count)
    {
        var faker = new Faker<Customer>()
            .RuleFor(c => c.Id, _ => Guid.NewGuid())
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.Email, (f, c) =>
                $"{c.FirstName.ToLower()}.{c.LastName.ToLower()}.{Guid.NewGuid():N}@{f.Internet.DomainName()}");

        const int batchSize = 5000;
        var generated = 0;

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            while (generated < count)
            {
                var take = Math.Min(batchSize, count - generated);
                var batch = faker.Generate(take);

                _dbContext.Customers.AddRange(batch);
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
