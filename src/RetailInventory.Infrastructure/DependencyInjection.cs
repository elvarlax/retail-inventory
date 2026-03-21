using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using RetailInventory.Application.Interfaces;
using RetailInventory.Infrastructure.Data;
using RetailInventory.Infrastructure.Repositories;
using RetailInventory.Infrastructure.Services;

namespace RetailInventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // EF Core write repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Dapper read repositories (query side of CQRS)
        services.AddScoped<IProductQueryRepository, ProductQueryRepository>();
        services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();
        services.AddScoped<IOrderQueryRepository, OrderQueryRepository>();

        // Outbox — stores domain events for async publishing
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Auth services
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        if (!environment.IsEnvironment("Testing"))
        {
            // PostgreSQL via EF Core (snake_case column naming)
            services.AddDbContext<RetailDbContext>(options =>
                options.UseNpgsql(
                        configuration.GetConnectionString("DefaultConnection"),
                        npgsql => npgsql.MigrationsAssembly("RetailInventory.Infrastructure"))
                    .UseSnakeCaseNamingConvention());

            // Shared connection pool for Dapper queries
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;
            services.AddSingleton(NpgsqlDataSource.Create(connectionString));

            // Background service that publishes outbox messages to Azure Service Bus
            services.AddHostedService<OutboxPublisher>();
        }

        return services;
    }
}
