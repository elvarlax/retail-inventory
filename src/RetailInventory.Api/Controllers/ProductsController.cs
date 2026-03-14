using Asp.Versioning;
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
    private readonly CreateProductHandler _createProductHandler;
    private readonly GetProductsHandler _getProductsHandler;
    private readonly GetProductByIdHandler _getProductByIdHandler;
    private readonly UpdateProductHandler _updateProductHandler;
    private readonly RestockProductHandler _restockProductHandler;
    private readonly DeleteProductHandler _deleteProductHandler;

    public ProductsController(
        CreateProductHandler createProductHandler,
        GetProductsHandler getProductsHandler,
        GetProductByIdHandler getProductByIdHandler,
        UpdateProductHandler updateProductHandler,
        RestockProductHandler restockProductHandler,
        DeleteProductHandler deleteProductHandler)
    {
        _createProductHandler = createProductHandler;
        _getProductsHandler = getProductsHandler;
        _getProductByIdHandler = getProductByIdHandler;
        _updateProductHandler = updateProductHandler;
        _restockProductHandler = restockProductHandler;
        _deleteProductHandler = deleteProductHandler;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.SKU,
            request.ImageUrl,
            request.Price,
            request.StockQuantity);

        var id = await _createProductHandler.Handle(command);

        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? search = null)
    {
        var query = new GetProductsQuery(pageNumber, pageSize, sortBy, sortDirection, search);
        var result = await _getProductsHandler.Handle(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _getProductByIdHandler.Handle(query);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request)
    {
        await _updateProductHandler.Handle(new UpdateProductCommand(
            id, request.Name, request.SKU, request.ImageUrl, request.Price, request.StockQuantity));
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/restock")]
    public async Task<IActionResult> Restock(Guid id, RestockProductRequest request)
    {
        await _restockProductHandler.Handle(new RestockProductCommand(id, request.Quantity));
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _deleteProductHandler.Handle(new DeleteProductCommand(id));
        return NoContent();
    }
}
