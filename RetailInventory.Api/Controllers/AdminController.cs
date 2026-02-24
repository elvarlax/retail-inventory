using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RetailInventory.Api.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet("secret")]
    public IActionResult Secret()
    {
        return Ok(new { message = "Admin access granted" });
    }
}