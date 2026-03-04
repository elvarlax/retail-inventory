using RetailInventory.Api.DTOs;
using RetailInventory.Api.Exceptions;
using RetailInventory.Api.Models;
using RetailInventory.Api.Repositories;

namespace RetailInventory.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthenticationResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return new AuthenticationResponseDto
        {
            AccessToken = _tokenService.CreateToken(user),
            TokenType = "Bearer",
            Role = user.Role
        };
    }

    public async Task<AuthenticationResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new ConflictException("Email already in use");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User"
        };

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        await _userRepository.AddAsync(user);
        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        return new AuthenticationResponseDto
        {
            AccessToken = _tokenService.CreateToken(user),
            TokenType = "Bearer",
            Role = user.Role
        };
    }
}
