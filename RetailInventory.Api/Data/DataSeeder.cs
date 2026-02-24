using RetailInventory.Api.Models;

namespace RetailInventory.Api.Data;

public static class DataSeeder
{
    public static void SeedUsers(RetailDbContext dbContext)
    {
        if (dbContext.Users.Any())
            return;

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

        dbContext.SaveChanges();
    }
}