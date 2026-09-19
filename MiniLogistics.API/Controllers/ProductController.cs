using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.Product;
using MiniLogistics.BLL.Services;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(
        IProductService productService)
    {
        _productService = productService;
    }

    // ========================================
    // GET: api/products
    // ========================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products =
            await _productService.GetAllAsync();

        return Ok(products);
    }

    // ========================================
    // GET: api/products/1
    // ========================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product =
            await _productService.GetByIdAsync(id);

        if (product == null)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        return Ok(product);
    }

    // ========================================
    // POST: api/products
    // ========================================
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductDTO request)
    {
        var product =
            await _productService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product
        );
    }

    // ========================================
    // PUT: api/products/1
    // ========================================
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductDTO request)
    {
        var product =
            await _productService.UpdateAsync(id, request);

        if (product == null)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        return Ok(product);
    }

    // ========================================
    // DELETE: api/products/1
    // ========================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await _productService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        return NoContent();
    }
}