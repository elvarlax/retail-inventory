using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Application.Products.Commands;
using RetailInventory.Application.Products.Queries;

namespace RetailInventory.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.SKU,
            request.ImageUrl,
            request.Price,
            request.StockQuantity);

        var id = await _sender.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new GetProductsQuery(pageNumber, pageSize, sortBy, sortDirection, search);
        var result = await _sender.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _sender.Send(new GetProductByIdQuery(id), ct);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        await _sender.Send(new UpdateProductCommand(
            id, request.Name, request.SKU, request.ImageUrl, request.Price, request.StockQuantity), ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/restock")]
    public async Task<IActionResult> Restock(Guid id, RestockProductRequest request, CancellationToken ct)
    {
        await _sender.Send(new RestockProductCommand(id, request.Quantity), ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteProductCommand(id), ct);
        return NoContent();
    }
}
