using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RetailInventory.Infrastructure.Data;
using RetailInventory.Infrastructure.Repositories;
using RetailInventory.Infrastructure.Services;
using RetailInventory.Api.Mappings;
using RetailInventory.Api.Middleware;
using RetailInventory.Application.Seed;
using RetailInventory.Application.Authentication;
using RetailInventory.Application.Mappings;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Customers.Commands;
using RetailInventory.Application.Customers.Queries;
using RetailInventory.Application.Orders.Commands;
using RetailInventory.Application.Orders.Queries;
using RetailInventory.Application.Products.Commands;
using RetailInventory.Application.Products.Queries;
using Npgsql;
using AppIProductRepository = RetailInventory.Application.Interfaces.IProductRepository;
using InfraProductRepository = RetailInventory.Infrastructure.Repositories.ProductRepository;
using AppICustomerRepository = RetailInventory.Application.Interfaces.ICustomerRepository;
using InfraCustomerRepository = RetailInventory.Infrastructure.Repositories.CustomerRepository;
using AppIOrderRepository = RetailInventory.Application.Interfaces.IOrderRepository;
using InfraOrderRepository = RetailInventory.Infrastructure.Repositories.OrderRepository;
using AppIUserRepository = RetailInventory.Application.Interfaces.IUserRepository;
using InfraUserRepository = RetailInventory.Infrastructure.Repositories.UserRepository;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Controllers
builder.Services.AddControllers();

// API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Clean Architecture handlers
builder.Services.AddScoped<AppIProductRepository, InfraProductRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IProductQueryRepository, ProductQueryRepository>();
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<GetProductsHandler>();
builder.Services.AddScoped<GetProductByIdHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<RestockProductHandler>();
builder.Services.AddScoped<DeleteProductHandler>();
builder.Services.AddScoped<AppICustomerRepository, InfraCustomerRepository>();
builder.Services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();
builder.Services.AddScoped<CreateCustomerHandler>();
builder.Services.AddScoped<GetCustomersHandler>();
builder.Services.AddScoped<GetCustomerByIdHandler>();
builder.Services.AddScoped<UpdateCustomerHandler>();
builder.Services.AddScoped<DeleteCustomerHandler>();
builder.Services.AddScoped<AppIOrderRepository, InfraOrderRepository>();
builder.Services.AddScoped<IOrderQueryRepository, OrderQueryRepository>();
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddScoped<CompleteOrderHandler>();
builder.Services.AddScoped<CancelOrderHandler>();
builder.Services.AddScoped<DeleteOrderHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();
builder.Services.AddScoped<GetOrdersHandler>();
builder.Services.AddScoped<GetOrderSummaryHandler>();
builder.Services.AddScoped<GetTopProductsHandler>();
builder.Services.AddScoped<AppIUserRepository, InfraUserRepository>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RegisterHandler>();

// AutoMapper — Api profile (DTOs → Commands) + Application profile (Commands → Entities)
builder.Services.AddAutoMapper(typeof(ApiMappingProfile), typeof(ApplicationMappingProfile));

// Services
builder.Services.AddScoped<ISeedService, SeedService>();

// Background Services
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<OutboxPublisher>();

// Database
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<RetailDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsql => npgsql.MigrationsAssembly("RetailInventory.Infrastructure"))
               .UseSnakeCaseNamingConvention();
    });

    // NpgsqlDataSource for Dapper queries (shared connection pool)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
}

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Retail Inventory API",
        Version = "v1",
        Description = "Backend API for retail inventory management — Clean Architecture, CQRS, transactional outbox pattern."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];

if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(jwtIssuer))
        throw new InvalidOperationException("Jwt:Issuer is missing");

    if (string.IsNullOrWhiteSpace(jwtAudience))
        throw new InvalidOperationException("Jwt:Audience is missing");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ValidateIssuer = builder.Environment.IsProduction(),
            ValidateAudience = builder.Environment.IsProduction(),
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
        };
    });

builder.Services.AddAuthorization();

// Build the app
var app = builder.Build();

// Serilog request logging
app.UseSerilogRequestLogging();

// Swagger in development only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Retail Inventory API v1");
        c.RoutePrefix = "swagger";
    });
}

// Global exception handling
app.UseMiddleware<ExceptionMiddleware>();

// Enforce HTTPS
app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Apply migrations automatically (not in Testing)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

    try
    {
        db.Database.Migrate();
        Log.Information("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration failed");
        throw;
    }
}

// Seed initial user data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();
    DataSeeder.SeedUsers(db);
}

// Run the app
app.Run();
