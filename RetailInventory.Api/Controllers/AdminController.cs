using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RetailInventory.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    [HttpGet("secret")]
    public IActionResult Secret()
    {
        return Ok(new { message = "Admin access granted" });
    }
}