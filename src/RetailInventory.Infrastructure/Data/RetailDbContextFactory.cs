using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RetailInventory.Infrastructure.Data;

public class RetailDbContextFactory : IDesignTimeDbContextFactory<RetailDbContext>
{
    public RetailDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Fallback for design-time migrations: build from individual env vars.
        // Set these in your shell or load from .env before running dotnet ef commands.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            static string Require(string key) =>
                Environment.GetEnvironmentVariable(key)
                ?? throw new InvalidOperationException(
                    $"Missing environment variable '{key}'. Set ConnectionStrings__DefaultConnection " +
                    "or all of POSTGRES_HOST, POSTGRES_PORT, POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD.");

            var host     = Require("POSTGRES_HOST");
            var port     = Require("POSTGRES_PORT");
            var database = Require("POSTGRES_DB");
            var username = Require("POSTGRES_USER");
            var password = Require("POSTGRES_PASSWORD");

            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        }

        var optionsBuilder = new DbContextOptionsBuilder<RetailDbContext>();

        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new RetailDbContext(optionsBuilder.Options);
    }
}
