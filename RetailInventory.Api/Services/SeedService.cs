using Bogus;
using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Services;

public class SeedService : ISeedService
{
    private static readonly string[] Categories =
        ["ELEC", "CLTH", "HOME", "SPRT", "FOOD", "BOOK", "TOYS", "AUTO"];

    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;

    public SeedService(
        ICustomerService customerService,
        IProductService productService,
        IOrderService orderService)
    {
        _customerService = customerService;
        _productService = productService;
        _orderService = orderService;
    }

    public async Task<SeedResultResponse> SeedAsync(int customers, int products, int orders)
    {
        var faker = new Faker();

        // Create customers — each call emits CustomerCreatedV1
        var customerIds = new List<Guid>();
        for (var i = 0; i < customers; i++)
        {
            var customer = await _customerService.CreateAsync(new RegisterRequestDto
            {
                FirstName = faker.Name.FirstName(),
                LastName = faker.Name.LastName(),
                // UUID suffix guarantees uniqueness across repeated seed calls
                Email = $"{faker.Internet.UserName()}.{Guid.NewGuid().ToString("N")[..8]}@{faker.Internet.DomainName()}",
                Password = "Seed1234!" // CustomerService ignores this field
            });
            customerIds.Add(customer.Id);
        }

        // Create products — each call emits ProductCreatedV1
        // StockQuantity is set high so orders never fail from insufficient stock
        var productIds = new List<Guid>();
        for (var i = 0; i < products; i++)
        {
            var id = await _productService.CreateAsync(new CreateProductRequest
            {
                Name = faker.Commerce.ProductName(),
                SKU = $"{faker.PickRandom(Categories)}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                ImageUrl = faker.Image.PicsumUrl(640, 480),
                Price = Math.Round(faker.Random.Decimal(1m, 500m), 2),
                StockQuantity = orders * 10
            });
            productIds.Add(id);
        }

        // Create orders — each call emits OrderPlacedV1
        // Then complete or cancel to match the 60/20/20 distribution,
        // each transition emitting OrderStatusChangedV1
        var statusEvents = 0;
        for (var i = 0; i < orders; i++)
        {
            var itemCount = faker.Random.Int(1, Math.Min(3, productIds.Count));
            var selectedProducts = faker.PickRandom(productIds, itemCount).Distinct().ToList();

            var orderId = await _orderService.CreateAsync(new CreateOrderRequest
            {
                CustomerId = faker.PickRandom(customerIds),
                Items = selectedProducts.Select(p => new CreateOrderItemRequest
                {
                    ProductId = p,
                    Quantity = faker.Random.Int(1, 3)
                }).ToList()
            });

            var roll = faker.Random.Int(1, 100);
            if (roll <= 60)
            {
                await _orderService.CompleteAsync(orderId);
                statusEvents++;
            }
            else if (roll <= 80)
            {
                await _orderService.CancelAsync(orderId);
                statusEvents++;
            }
            // roll > 80: order stays Pending
        }

        return new SeedResultResponse
        {
            Customers = customers,
            Products = products,
            Orders = orders,
            EventsEmitted = customers + products + orders + statusEvents
        };
    }
}
