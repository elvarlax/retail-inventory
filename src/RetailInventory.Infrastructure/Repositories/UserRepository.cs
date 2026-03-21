using Microsoft.EntityFrameworkCore;
using RetailInventory.Application.Interfaces;
using RetailInventory.Domain;
using RetailInventory.Infrastructure.Data;

namespace RetailInventory.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly RetailDbContext _dbContext;

    public UserRepository(RetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _dbContext.Users.AddAsync(user, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default)
    {
        return await _dbContext.Users.AnyAsync(u => u.Email == email, ct);
    }

    public Task DeleteAsync(User user, CancellationToken ct = default)
    {
        _dbContext.Users.Remove(user);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
