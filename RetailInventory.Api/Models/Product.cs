namespace RetailInventory.Api.Models
{
    public class Product
    {
        public Guid Id { get; set; }

        // Used only for initial DummyJSON import
        public int ExternalId { get; set; }
        public required string Name { get; set; }
        public required string SKU { get; set; }
        public int StockQuantity { get; set; }
        public decimal Price { get; set; }
    }
}
