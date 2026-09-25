using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.ProductVariant;
using MiniLogistics.BLL.Services;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/product-variants")]
public class ProductVariantController : ControllerBase
{
    private readonly IProductVariantService _productVariantService;

    public ProductVariantController(
        IProductVariantService productVariantService)
    {
        _productVariantService = productVariantService;
    }


    // =====================================================
    // GET ALL
    // PUBLIC
    // PAGINATION
    // =====================================================

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] ProductVariantPaginationRequestDTO request)
    {
        var result =
            await _productVariantService.GetAllAsync(request);

        return Ok(result);
    }


    // =====================================================
    // GET BY ID
    // PUBLIC
    // =====================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        var result =
            await _productVariantService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }


    // =====================================================
    // GET BY PRODUCT
    // PUBLIC
    // =====================================================

    [HttpGet("product/{productId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByProductId(
        long productId)
    {
        var result =
            await _productVariantService
                .GetByProductIdAsync(productId);

        return Ok(result);
    }


    // =====================================================
    // CREATE
    // SELLER
    // =====================================================

    [Authorize(Roles = "seller")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductVariantDTO request)
    {
        var userId = GetCurrentUserId();

        var result =
            await _productVariantService.CreateAsync(
                userId,
                request);

        return Ok(result);
    }


    // =====================================================
    // UPDATE
    // SELLER
    // =====================================================

    [Authorize(Roles = "seller")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateProductVariantDTO request)
    {
        var userId = GetCurrentUserId();

        var result =
            await _productVariantService.UpdateAsync(
                userId,
                id,
                request);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }


    // =====================================================
    // DELETE
    // SELLER
    // =====================================================

    [Authorize(Roles = "seller")]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var userId = GetCurrentUserId();

        var result =
            await _productVariantService.DeleteAsync(
                userId,
                id);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }


    // =====================================================
    // GET CURRENT USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                userIdClaim,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được UserId.");
        }

        return userId;
    }
}