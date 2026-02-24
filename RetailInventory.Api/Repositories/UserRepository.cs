using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly RetailDbContext _dbContext;

        public UserRepository(RetailDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}