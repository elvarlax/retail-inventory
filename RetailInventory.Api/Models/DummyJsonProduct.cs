namespace RetailInventory.Api.Models;

public class DummyJsonProduct
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}