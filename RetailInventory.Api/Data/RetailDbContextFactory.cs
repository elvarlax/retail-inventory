using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RetailInventory.Api.Data;

public class RetailDbContextFactory : IDesignTimeDbContextFactory<RetailDbContext>
{
    public RetailDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["ConnectionStrings__DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ConnectionStrings__DefaultConnection' was not found."
            );
        }

        var optionsBuilder = new DbContextOptionsBuilder<RetailDbContext>();

        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new RetailDbContext(optionsBuilder.Options);
    }
}