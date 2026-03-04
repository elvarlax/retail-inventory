using System.ComponentModel.DataAnnotations;

namespace RetailInventory.Api.DTOs;

public class SeedRequest
{
    [Range(1, 1000, ErrorMessage = "Customers must be between 1 and 1000.")]
    public int Customers { get; set; } = 10;

    [Range(1, 1000, ErrorMessage = "Products must be between 1 and 1000.")]
    public int Products { get; set; } = 10;

    [Range(1, 5000, ErrorMessage = "Orders must be between 1 and 5000.")]
    public int Orders { get; set; } = 20;
}
