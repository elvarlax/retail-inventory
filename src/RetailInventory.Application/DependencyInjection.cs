using Microsoft.Extensions.DependencyInjection;
using RetailInventory.Application.Authentication.Commands;
using RetailInventory.Application.Seed;

namespace RetailInventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(LoginHandler).Assembly));

        services.AddScoped<ISeedService, SeedService>();

        return services;
    }
}
