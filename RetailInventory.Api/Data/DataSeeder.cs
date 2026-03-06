using RetailInventory.Api.Models;

namespace RetailInventory.Api.Data;

public static class DataSeeder
{
    public static void SeedUsers(RetailDbContext dbContext)
    {
        if (!dbContext.Users.Any())
        {
            dbContext.Users.AddRange(
                new User
                {
                    Email = "admin@local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = "Admin"
                },
                new User
                {
                    Email = "user@local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                    Role = "User"
                }
            );
        }

        if (!dbContext.Customers.Any())
        {
            dbContext.Customers.AddRange(
                new Customer
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@local"
                },
                new Customer
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Regular",
                    LastName = "User",
                    Email = "user@local"
                }
            );
        }

        dbContext.SaveChanges();
    }
}
