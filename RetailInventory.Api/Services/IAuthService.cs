using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Services;

public interface IAuthService
{
    Task<AuthenticationResponseDto?> LoginAsync(LoginRequestDto request);
    Task<AuthenticationResponseDto> RegisterAsync(RegisterRequestDto request);
}
