using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.ProductImage;
using MiniLogistics.BLL.Services.ProductImage;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/product-images")]
[Authorize]
public class ProductImageController : ControllerBase
{
    private readonly IProductImageService _service;

    public ProductImageController(
        IProductImageService service)
    {
        _service = service;
    }

    // =====================================================
    // GET BY PRODUCT
    // CUSTOMER / SELLER / ADMIN
    // =====================================================

    [HttpGet("product/{productId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByProduct(
        long productId)
    {
        var result =
            await _service.GetByProductIdAsync(productId);

        return Ok(result);
    }

    // =====================================================
    // GET BY ID
    // =====================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(
        long id)
    {
        var result =
            await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    // =====================================================
    // CREATE
    // ADMIN / SELLER
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductImageDTO request)
    {
        long userId = GetCurrentUserId();

        string role = GetCurrentRole();

        var result =
            await _service.CreateAsync(
                userId,
                role,
                request);

        return Ok(result);
    }

    // =====================================================
    // UPDATE
    // ADMIN / SELLER
    // =====================================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateProductImageDTO request)
    {
        long userId = GetCurrentUserId();

        string role = GetCurrentRole();

        var result =
            await _service.UpdateAsync(
                id,
                userId,
                role,
                request);

        return Ok(result);
    }

    // =====================================================
    // DELETE
    // ADMIN / SELLER
    // =====================================================

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Delete(
        long id)
    {
        long userId = GetCurrentUserId();

        string role = GetCurrentRole();

        await _service.DeleteAsync(
            id,
            userId,
            role);

        return NoContent();
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private long GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                value,
                out long userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được User ID.");
        }

        return userId;
    }

    private string GetCurrentRole()
    {
        var role =
            User.FindFirstValue(
                ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được Role.");
        }

        return role;
    }
}