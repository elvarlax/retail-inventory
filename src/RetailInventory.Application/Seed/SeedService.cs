using Bogus;
using MediatR;
using RetailInventory.Application.Customers.Commands;
using RetailInventory.Application.Orders.Commands;
using RetailInventory.Application.Products.Commands;

namespace RetailInventory.Application.Seed;

public class SeedService : ISeedService
{
    private static readonly string[] Categories =
        ["ELEC", "CLTH", "HOME", "SPRT", "FOOD", "BOOK", "TOYS", "AUTO"];

    private readonly ISender _sender;

    public SeedService(ISender sender)
    {
        _sender = sender;
    }

    public async Task<SeedResult> SeedAsync(int customers, int products, int orders)
    {
        var faker = new Faker();

        var customerIds = new List<Guid>();
        for (var i = 0; i < customers; i++)
        {
            var customer = await _sender.Send(new CreateCustomerCommand(
                FirstName: faker.Name.FirstName(),
                LastName: faker.Name.LastName(),
                Email: $"{faker.Internet.UserName()}.{Guid.NewGuid().ToString("N")[..8]}@{faker.Internet.DomainName()}"
            ));
            customerIds.Add(customer.Id);
        }

        var productIds = new List<Guid>();
        for (var i = 0; i < products; i++)
        {
            var id = await _sender.Send(new CreateProductCommand(
                Name: $"{faker.Commerce.ProductName()} {Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                SKU: $"{faker.PickRandom(Categories)}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                ImageUrl: faker.Image.PicsumUrl(640, 480),
                Price: Math.Round(faker.Random.Decimal(1m, 500m), 2),
                StockQuantity: orders * 10
            ));
            productIds.Add(id);
        }

        var statusEvents = 0;
        for (var i = 0; i < orders; i++)
        {
            var itemCount = faker.Random.Int(1, Math.Min(3, productIds.Count));
            var selectedProducts = faker.PickRandom(productIds, itemCount).Distinct().ToList();

            var orderId = await _sender.Send(new PlaceOrderCommand(
                CustomerId: faker.PickRandom(customerIds),
                Items: selectedProducts.Select(p => new OrderItemRequest(
                    ProductId: p,
                    Quantity: faker.Random.Int(1, 3)
                )).ToList()
            ));

            var roll = faker.Random.Int(1, 100);
            if (roll <= 60)
            {
                await _sender.Send(new CompleteOrderCommand(orderId));
                statusEvents++;
            }
            else if (roll <= 80)
            {
                await _sender.Send(new CancelOrderCommand(orderId));
                statusEvents++;
            }
        }

        return new SeedResult
        {
            Customers = customers,
            Products = products,
            Orders = orders,
            EventsEmitted = customers + products + orders + statusEvents
        };
    }
}
