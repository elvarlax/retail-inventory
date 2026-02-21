using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Services;

namespace RetailInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var orderId = await _orderService.CreateAsync(request);
        return Ok(new { orderId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);
        return Ok(order);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _orderService.CompleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _orderService.CancelAsync(id);
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _orderService.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var result = await _orderService.GetPagedAsync(pageNumber, pageSize, status);
        return Ok(result);
    }
}