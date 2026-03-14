using RetailInventory.Application.Authentication.DTOs;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Customers.Events;
using RetailInventory.Application.Interfaces;
using RetailInventory.Application.Outbox;
using RetailInventory.Domain;
using System.Text.Json;

namespace RetailInventory.Application.Authentication;

public class RegisterHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterHandler(
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        IOutboxRepository outboxRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _outboxRepository = outboxRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand command)
    {
        if (await _userRepository.ExistsAsync(command.Email))
            throw new ConflictException("Email already in use.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = command.FirstName,
            LastName = command.LastName,
            Email = command.Email
        };

        var user = new User
        {
            Email = command.Email,
            PasswordHash = _passwordHasher.Hash(command.Password),
            Role = "User"
        };

        var occurredAt = DateTime.UtcNow;

        var @event = new CustomerCreatedV1
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            CustomerId = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email
        };

        await _customerRepository.AddAsync(customer);
        await _userRepository.AddAsync(user);
        await _outboxRepository.AddAsync(new OutboxEntry(
            Id: @event.EventId,
            Type: nameof(CustomerCreatedV1),
            Source: OutboxConstants.Source,
            Payload: JsonSerializer.Serialize(@event),
            OccurredAtUtc: occurredAt
        ));

        // Single SaveChangesAsync commits customer, user, and outbox atomically.
        // All three repositories share the same DbContext instance (scoped DI).
        await _userRepository.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = _tokenService.CreateToken(user, customer.Id),
            TokenType = "Bearer",
            Role = user.Role,
            CustomerId = customer.Id,
            FirstName = customer.FirstName
        };
    }
}
