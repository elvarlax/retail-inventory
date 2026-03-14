using Asp.Versioning;
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
    private readonly PlaceOrderHandler _placeOrderHandler;
    private readonly CompleteOrderHandler _completeOrderHandler;
    private readonly CancelOrderHandler _cancelOrderHandler;
    private readonly DeleteOrderHandler _deleteOrderHandler;
    private readonly GetOrderByIdHandler _getOrderByIdHandler;
    private readonly GetOrdersHandler _getOrdersHandler;
    private readonly GetOrderSummaryHandler _getOrderSummaryHandler;
    private readonly GetTopProductsHandler _getTopProductsHandler;

    public OrdersController(
        PlaceOrderHandler placeOrderHandler,
        CompleteOrderHandler completeOrderHandler,
        CancelOrderHandler cancelOrderHandler,
        DeleteOrderHandler deleteOrderHandler,
        GetOrderByIdHandler getOrderByIdHandler,
        GetOrdersHandler getOrdersHandler,
        GetOrderSummaryHandler getOrderSummaryHandler,
        GetTopProductsHandler getTopProductsHandler)
    {
        _placeOrderHandler = placeOrderHandler;
        _completeOrderHandler = completeOrderHandler;
        _cancelOrderHandler = cancelOrderHandler;
        _deleteOrderHandler = deleteOrderHandler;
        _getOrderByIdHandler = getOrderByIdHandler;
        _getOrdersHandler = getOrdersHandler;
        _getOrderSummaryHandler = getOrderSummaryHandler;
        _getTopProductsHandler = getTopProductsHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(PlaceOrderCommand command)
    {
        var orderId = await _placeOrderHandler.Handle(command);
        return CreatedAtAction(nameof(GetById), new { id = orderId }, new { OrderId = orderId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _getOrderByIdHandler.Handle(new GetOrderByIdQuery(id));
        var requestingCustomerId = GetRequestingCustomerId();
        if (requestingCustomerId.HasValue && order.CustomerId != requestingCustomerId.Value)
            throw new ForbiddenException("You can only view your own orders.");
        return Ok(order);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _completeOrderHandler.Handle(new CompleteOrderCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _cancelOrderHandler.Handle(new CancelOrderCommand(id, GetRequestingCustomerId()));
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _deleteOrderHandler.Handle(new DeleteOrderCommand(id));
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _getOrderSummaryHandler.Handle(new GetOrderSummaryQuery());
        return Ok(summary);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 5)
    {
        var result = await _getTopProductsHandler.Handle(new GetTopProductsQuery(limit));
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
        [FromQuery] string? search = null)
    {
        // Non-admins can only see their own orders
        var requestingCustomerId = GetRequestingCustomerId();
        if (requestingCustomerId.HasValue)
        {
            customerId = requestingCustomerId;
            search = null; // users can't search across customers
        }

        var result = await _getOrdersHandler.Handle(new GetOrdersQuery(
            pageNumber, pageSize, status, sortBy, sortDirection, customerId, search));
        return Ok(result);
    }
}
