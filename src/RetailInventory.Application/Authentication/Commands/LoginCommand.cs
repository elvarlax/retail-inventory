using MediatR;
using RetailInventory.Application.Authentication.DTOs;

namespace RetailInventory.Application.Authentication.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto?>;
