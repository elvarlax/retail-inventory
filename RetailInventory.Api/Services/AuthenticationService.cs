using RetailInventory.Api.Data;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Exceptions;

namespace RetailInventory.Api.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserService _userService;
    private readonly ICustomerService _customerService;
    private readonly ITokenService _tokenService;
    private readonly RetailDbContext _dbContext;

    public AuthenticationService(
        IUserService userService,
        ICustomerService customerService,
        ITokenService tokenService,
        RetailDbContext dbContext)
    {
        _userService = userService;
        _customerService = customerService;
        _tokenService = tokenService;
        _dbContext = dbContext;
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

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var customer = await _customerService.CreateAsync(request);
        var user = await _userService.CreateAsync(request);

        await transaction.CommitAsync();

        return new AuthenticationResponseDto
        {
            AccessToken = _tokenService.CreateToken(user, customer.Id),
            TokenType = "Bearer",
            Role = user.Role
        };
    }
}
