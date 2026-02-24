using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Repositories;
using RetailInventory.Api.Services;

namespace RetailInventory.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public AuthenticationController(
            IUserRepository userRepository,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        private IActionResult InvalidCredentials()
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
                return InvalidCredentials();

            var valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!valid)
                return InvalidCredentials();

            var token = _tokenService.CreateToken(user);

            var response = new AuthenticationResponseDto
            {
                AccessToken = token,
                TokenType = "Bearer",
                Role = user.Role
            };

            return Ok(response);
        }
    }
}