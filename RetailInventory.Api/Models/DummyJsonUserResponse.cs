using RetailInventory.Api.Models;

public class DummyJsonUserResponse
{
    public List<DummyJsonUser> Users { get; set; } = new List<DummyJsonUser>();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}