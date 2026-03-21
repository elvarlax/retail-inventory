using MediatR;
using RetailInventory.Application.Authentication.DTOs;

namespace RetailInventory.Application.Authentication.Commands;

public record RegisterCommand(string FirstName, string LastName, string Email, string Password) : IRequest<AuthResponseDto>;
