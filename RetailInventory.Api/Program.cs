using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.Services;
using RetailInventory.Api.Repositories;
using RetailInventory.Api.Middleware;
using RetailInventory.Api.Mappings;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Database
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<RetailDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
}

// HTTP Client for DummyJson API
builder.Services.AddHttpClient<IDummyJsonService, DummyJsonService>(client =>
{
    client.BaseAddress = new Uri("https://dummyjson.com/");
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Apply migrations automatically (not in Testing)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RetailDbContext>();

    var retries = 10;
    while (retries > 0)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch
        {
            retries--;
            Thread.Sleep(3000);
        }
    }

    if (retries == 0)
        throw new Exception("Database migration failed after multiple retries.");
}

app.Run();