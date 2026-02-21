using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories;

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

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _dbContext.Customers.AsNoTracking().ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Customer customer)
    {
        await _dbContext.Customers.AddAsync(customer);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}