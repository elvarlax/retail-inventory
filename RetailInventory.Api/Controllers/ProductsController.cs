using Microsoft.AspNetCore.Mvc;
using RetailInventory.Api.DTOs;
using RetailInventory.Api.Services;

namespace RetailInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportProducts()
    {
        var count = await _productService.ImportFromExternalAsync();

        return Ok(new ImportResultResponse
        {
            ImportedCount = count
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _productService.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }
}