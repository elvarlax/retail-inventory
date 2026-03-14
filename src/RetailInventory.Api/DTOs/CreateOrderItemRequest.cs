using System.ComponentModel.DataAnnotations;

namespace RetailInventory.Api.DTOs;

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }
}