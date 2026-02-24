namespace RetailInventory.Api.DTOs;

public class AuthenticationResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public string Role { get; set; } = null!;
}