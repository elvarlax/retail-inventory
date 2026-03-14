using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Application.Authentication;
using RetailInventory.Application.Authentication.DTOs;

namespace RetailInventory.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly LoginHandler _loginHandler;
    private readonly RegisterHandler _registerHandler;
    private readonly IMapper _mapper;

    public AuthenticationController(LoginHandler loginHandler, RegisterHandler registerHandler, IMapper mapper)
    {
        _loginHandler = loginHandler;
        _registerHandler = registerHandler;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var command = _mapper.Map<LoginCommand>(request);
        var response = await _loginHandler.Handle(command);

        if (response == null)
            return Unauthorized(new { message = "Invalid credentials" });

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var command = _mapper.Map<RegisterCommand>(request);
        var response = await _registerHandler.Handle(command);

        return Ok(response);
    }
}
