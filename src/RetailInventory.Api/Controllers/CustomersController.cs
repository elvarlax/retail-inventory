using Asp.Versioning;
using MediatR;
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
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var command = new CreateCustomerCommand(request.FirstName, request.LastName, request.Email);
        var customer = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, null);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new GetCustomersQuery(pageNumber, pageSize, sortBy, sortDirection, search);
        var result = await _sender.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !IsOwnRecord(id))
            return Forbid();

        var customer = await _sender.Send(new GetCustomerByIdQuery(id), ct);

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !IsOwnRecord(id))
            return Forbid();

        await _sender.Send(new UpdateCustomerCommand(
            id, request.FirstName, request.LastName, request.Email), ct);
        return NoContent();
    }

    private bool IsOwnRecord(Guid id)
    {
        var claim = User.FindFirst("customerId")?.Value;
        return Guid.TryParse(claim, out var customerId) && customerId == id;
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteCustomerCommand(id), ct);
        return NoContent();
    }
}
