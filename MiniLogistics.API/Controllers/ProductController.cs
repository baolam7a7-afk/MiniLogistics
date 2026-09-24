using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Product;
using MiniLogistics.BLL.Services.Product;

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

    // ==========================================
    // GET ALL
    // PUBLIC
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
    // PUBLIC
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
    // PUBLIC
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
    // PUBLIC
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
    // SELLER ONLY
    // ==========================================

    [HttpPost]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductDTO request)
    {
        var userId =
            GetCurrentUserId();

        var product =
            await _productService.CreateAsync(
                userId,
                request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    // ==========================================
    // UPDATE
    // SELLER ONLY
    // ==========================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateProductDTO request)
    {
        var userId =
            GetCurrentUserId();

        var product =
            await _productService.UpdateAsync(
                userId,
                id,
                request);

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
    // SELLER ONLY
    // ==========================================

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> Delete(
        long id)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _productService.DeleteAsync(
                userId,
                id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Product không tồn tại."
            });
        }

        return NoContent();
    }

    // ==========================================
    // GET CURRENT USER ID
    // ==========================================

    private long GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "User ID không tồn tại trong JWT.");
        }

        if (!long.TryParse(
                userId,
                out var parsedUserId))
        {
            throw new UnauthorizedAccessException(
                "User ID trong JWT không hợp lệ.");
        }

        return parsedUserId;
    }
}