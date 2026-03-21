using MediatR;
using RetailInventory.Application.Authentication.DTOs;
using RetailInventory.Application.Interfaces;

namespace RetailInventory.Application.Authentication.Commands;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginHandler(
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto?> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, ct);
        if (user == null) return null;

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
            return null;

        var customer = await _customerRepository.GetByEmailAsync(command.Email, ct);

        return new AuthResponseDto
        {
            AccessToken = _tokenService.CreateToken(user, customer?.Id),
            TokenType = "Bearer",
            Role = user.Role,
            CustomerId = customer?.Id,
            FirstName = customer?.FirstName
        };
    }
}
