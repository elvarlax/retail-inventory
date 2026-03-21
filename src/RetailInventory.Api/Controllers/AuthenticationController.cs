using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Application.Authentication.Commands;

namespace RetailInventory.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public AuthenticationController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var command = _mapper.Map<LoginCommand>(request);
        var response = await _sender.Send(command, ct);

        if (response == null)
            return Unauthorized(new { message = "Invalid credentials" });

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var command = _mapper.Map<RegisterCommand>(request);
        var response = await _sender.Send(command, ct);

        return Ok(response);
    }
}
