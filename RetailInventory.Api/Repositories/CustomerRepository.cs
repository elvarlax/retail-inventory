using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;

namespace RetailInventory.Api.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly RetailDbContext _dbContext;

        public CustomerRepository(RetailDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ExistsByExternalIdAsync(int externalId)
        {
            return await _dbContext.Customers.AnyAsync(c => c.ExternalId == externalId);
        }

        public async Task AddAsync(Models.Customer customer)
        {
            await _dbContext.Customers.AddAsync(customer);
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}