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

        var response = new CreateOrderResponse
        {
            OrderId = orderId
        };

        return CreatedAtAction(nameof(GetById), new { id = orderId }, response);
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
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "desc")
    {
        var result = await _orderService.GetPagedAsync(
            pageNumber,
            pageSize,
            status,
            sortBy,
            sortDirection);
        return Ok(result);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateOrders(GenerateOrdersRequest request)
    {
        if (request.Count is < 1 or > 1000)
            return BadRequest("Count must be between 1 and 1000.");

        await _orderService.GenerateRandomOrdersAsync(request.Count);

        return Ok(new ImportResultResponse
        {
            ImportedCount = request.Count
        });
    }
}