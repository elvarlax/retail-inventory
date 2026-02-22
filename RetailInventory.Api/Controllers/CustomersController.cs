using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Services;

namespace RetailInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportCustomers()
    {
        var count = await _customerService.ImportFromExternalAsync();

        return Ok(new ImportResultResponse
        {
            ImportedCount = count
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCustomers()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await _customerService.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }
}
