namespace RetailInventory.Application.Authentication.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public string Role { get; set; } = null!;
    public Guid? CustomerId { get; set; }
    public string? FirstName { get; set; }
}
