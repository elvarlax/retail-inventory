using Microsoft.EntityFrameworkCore;
using RetailInventory.Application.Interfaces;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Data;

namespace RetailInventory.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly RetailDbContext _dbContext;

    public CustomerRepository(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        await _dbContext.Customers.AddAsync(customer, ct);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Customers.AnyAsync(c => c.Id == id, ct);
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Customers.FindAsync([id], ct);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower(), ct);
    }

    public Task DeleteAsync(Customer customer, CancellationToken ct = default)
    {
        _dbContext.Customers.Remove(customer);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
