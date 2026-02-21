namespace RetailInventory.Api.DTOs;

public class OrderSummaryDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PendingRevenue { get; set; }
}