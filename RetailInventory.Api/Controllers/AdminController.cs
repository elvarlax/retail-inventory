using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Services;

namespace RetailInventory.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly ISeedService _seedService;

    public AdminController(ISeedService seedService)
    {
        _seedService = seedService;
    }

    [HttpGet("secret")]
    public IActionResult Secret()
    {
        return Ok(new { message = "Admin access granted" });
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed([FromBody] SeedRequest request)
    {
        var result = await _seedService.SeedAsync(request.Customers, request.Products, request.Orders);
        return Ok(result);
    }
}
