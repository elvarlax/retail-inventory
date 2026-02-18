namespace RetailInventory.Api.Models;

public class DummyJsonProductResponse
{
    public List<DummyJsonProduct> Products { get; set; } = new List<DummyJsonProduct>();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}