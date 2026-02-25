using System.ComponentModel.DataAnnotations;

namespace RetailInventory.Api.DTOs;

public class GenerateRequest
{
    [Range(1, 100000, ErrorMessage = "Count must be between 1 and 100000.")]
    public int Count { get; set; }
}
