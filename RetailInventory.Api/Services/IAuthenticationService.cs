using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResponseDto?> LoginAsync(LoginRequestDto request);
    Task<AuthenticationResponseDto> RegisterAsync(RegisterRequestDto request);
}
