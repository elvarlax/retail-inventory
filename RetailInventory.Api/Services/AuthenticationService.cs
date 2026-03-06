using RetailInventory.Api.DTOs;
using RetailInventory.Api.Exceptions;

namespace RetailInventory.Api.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserService _userService;
    private readonly ICustomerService _customerService;
    private readonly ITokenService _tokenService;

    public AuthenticationService(
        IUserService userService,
        ICustomerService customerService,
        ITokenService tokenService)
    {
        _userService = userService;
        _customerService = customerService;
        _tokenService = tokenService;
    }

    public async Task<AuthenticationResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var customer = await _customerService.GetByEmailAsync(request.Email);

        return new AuthenticationResponseDto
        {
            AccessToken = _tokenService.CreateToken(user, customer?.Id),
            TokenType = "Bearer",
            Role = user.Role
        };
    }

    public async Task<AuthenticationResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existing = await _userService.GetByEmailAsync(request.Email);

        if (existing != null)
            throw new ConflictException("Email already in use");

        var customer = await _customerService.CreateAsync(request);
        var user = await _userService.CreateAsync(request);

        return new AuthenticationResponseDto
        {
            AccessToken = _tokenService.CreateToken(user, customer.Id),
            TokenType = "Bearer",
            Role = user.Role
        };
    }
}
