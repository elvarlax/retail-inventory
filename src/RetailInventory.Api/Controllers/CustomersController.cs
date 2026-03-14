using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Application.Customers.Commands;
using RetailInventory.Application.Customers.Queries;

namespace RetailInventory.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CreateCustomerHandler _createCustomerHandler;
    private readonly GetCustomersHandler _getCustomersHandler;
    private readonly GetCustomerByIdHandler _getCustomerByIdHandler;
    private readonly UpdateCustomerHandler _updateCustomerHandler;
    private readonly DeleteCustomerHandler _deleteCustomerHandler;

    public CustomersController(
        CreateCustomerHandler createCustomerHandler,
        GetCustomersHandler getCustomersHandler,
        GetCustomerByIdHandler getCustomerByIdHandler,
        UpdateCustomerHandler updateCustomerHandler,
        DeleteCustomerHandler deleteCustomerHandler)
    {
        _createCustomerHandler = createCustomerHandler;
        _getCustomersHandler = getCustomersHandler;
        _getCustomerByIdHandler = getCustomerByIdHandler;
        _updateCustomerHandler = updateCustomerHandler;
        _deleteCustomerHandler = deleteCustomerHandler;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerRequest request)
    {
        var command = new CreateCustomerCommand(request.FirstName, request.LastName, request.Email);
        var customer = await _createCustomerHandler.Handle(command);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, null);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? search = null)
    {
        var query = new GetCustomersQuery(pageNumber, pageSize, sortBy, sortDirection, search);
        var result = await _getCustomersHandler.Handle(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!User.IsInRole("Admin") && !IsOwnRecord(id))
            return Forbid();

        var query = new GetCustomerByIdQuery(id);
        var customer = await _getCustomerByIdHandler.Handle(query);

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerRequest request)
    {
        if (!User.IsInRole("Admin") && !IsOwnRecord(id))
            return Forbid();

        await _updateCustomerHandler.Handle(new UpdateCustomerCommand(
            id, request.FirstName, request.LastName, request.Email));
        return NoContent();
    }

    private bool IsOwnRecord(Guid id)
    {
        var claim = User.FindFirst("customerId")?.Value;
        return Guid.TryParse(claim, out var customerId) && customerId == id;
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _deleteCustomerHandler.Handle(new DeleteCustomerCommand(id));
        return NoContent();
    }
}
