using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.Product;
using MiniLogistics.BLL.Services.Product;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    // ==========================================
    // GET ALL
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products =
            await _productService.GetAllAsync();

        return Ok(products);
    }

    // ==========================================
    // GET BY ID
    // ==========================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var product =
            await _productService.GetByIdAsync(id);

        if (product == null)
        {
            return NotFound(new
            {
                message = "Product không tồn tại."
            });
        }

        return Ok(product);
    }

    // ==========================================
    // GET BY CATEGORY
    // ==========================================

    [HttpGet("category/{categoryId:long}")]
    public async Task<IActionResult> GetByCategory(
        long categoryId)
    {
        var products =
            await _productService
                .GetByCategoryAsync(categoryId);

        return Ok(products);
    }

    // ==========================================
    // SEARCH
    // ==========================================

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string keyword)
    {
        var products =
            await _productService
                .SearchAsync(keyword);

        return Ok(products);
    }

    // ==========================================
    // CREATE
    // ==========================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductDTO request)
    {
        var product =
            await _productService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    // ==========================================
    // UPDATE
    // ==========================================

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateProductDTO request)
    {
        var product =
            await _productService
                .UpdateAsync(id, request);

        if (product == null)
        {
            return NotFound(new
            {
                message = "Product không tồn tại."
            });
        }

        return Ok(product);
    }

    // ==========================================
    // DELETE
    // ==========================================

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result =
            await _productService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Product không tồn tại."
            });
        }

        return NoContent();
    }
}