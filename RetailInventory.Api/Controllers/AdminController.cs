using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.Data;
using RetailInventory.Api.DTOs;

namespace RetailInventory.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly CustomerGenerator _customerGenerator;
    private readonly ProductGenerator _productGenerator;
    private readonly OrderGenerator _orderGenerator;

    public AdminController(
        CustomerGenerator customerGenerator,
        ProductGenerator productGenerator,
        OrderGenerator orderGenerator)
    {
        _customerGenerator = customerGenerator;
        _productGenerator = productGenerator;
        _orderGenerator = orderGenerator;
    }

    [HttpGet("secret")]
    public IActionResult Secret()
    {
        return Ok(new { message = "Admin access granted" });
    }

    [HttpPost("generate/customers")]
    public async Task<IActionResult> GenerateCustomers([FromBody] GenerateRequest request)
    {
        if (request.Count < 1)
            return BadRequest("Count must be at least 1.");

        var generated = await _customerGenerator.GenerateAsync(request.Count);
        return Ok(new GenerateResultResponse { GeneratedCount = generated });
    }

    [HttpPost("generate/products")]
    public async Task<IActionResult> GenerateProducts([FromBody] GenerateRequest request)
    {
        if (request.Count < 1)
            return BadRequest("Count must be at least 1.");

        var generated = await _productGenerator.GenerateAsync(request.Count);
        return Ok(new GenerateResultResponse { GeneratedCount = generated });
    }

    [HttpPost("generate/orders")]
    public async Task<IActionResult> GenerateOrders([FromBody] GenerateRequest request)
    {
        if (request.Count < 1)
            return BadRequest("Count must be at least 1.");

        var generated = await _orderGenerator.GenerateAsync(request.Count);
        return Ok(new GenerateResultResponse { GeneratedCount = generated });
    }
}
