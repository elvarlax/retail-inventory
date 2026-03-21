using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailInventory.Application.Common.Exceptions;
using RetailInventory.Application.Orders.Commands;
using RetailInventory.Application.Orders.Queries;

namespace RetailInventory.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create(PlaceOrderCommand command, CancellationToken ct)
    {
        var orderId = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = orderId }, new { OrderId = orderId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(id), ct);
        var requestingCustomerId = GetRequestingCustomerId();
        if (requestingCustomerId.HasValue && order.CustomerId != requestingCustomerId.Value)
            throw new ForbiddenException("You can only view your own orders.");
        return Ok(order);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new CompleteOrderCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _sender.Send(new CancelOrderCommand(id, GetRequestingCustomerId()), ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteOrderCommand(id), ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await _sender.Send(new GetOrderSummaryQuery(), ct);
        return Ok(summary);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 5, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetTopProductsQuery(limit), ct);
        return Ok(result);
    }

    // Returns the customerId claim for regular users; null for admins (no restriction).
    private Guid? GetRequestingCustomerId()
    {
        if (User.IsInRole("Admin")) return null;
        var claim = User.FindFirst("customerId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        // Non-admins can only see their own orders
        var requestingCustomerId = GetRequestingCustomerId();
        if (requestingCustomerId.HasValue)
        {
            customerId = requestingCustomerId;
            search = null; // users can't search across customers
        }

        var result = await _sender.Send(new GetOrdersQuery(
            pageNumber, pageSize, status, sortBy, sortDirection, customerId, search), ct);
        return Ok(result);
    }
}
