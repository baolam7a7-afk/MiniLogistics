using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.ProductVariant;
using MiniLogistics.BLL.Services;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/product-variants")]
public class ProductVariantController : ControllerBase
{
    private readonly IProductVariantService _variantService;


    public ProductVariantController(
        IProductVariantService variantService)
    {
        _variantService = variantService;
    }


    // =====================================================
    // GET ALL
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var variants =
            await _variantService.GetAllAsync();

        return Ok(variants);
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var variant =
            await _variantService.GetByIdAsync(id);

        if (variant == null)
        {
            return NotFound(new
            {
                message = "Product variant not found."
            });
        }

        return Ok(variant);
    }


    // =====================================================
    // GET BY PRODUCT
    // =====================================================

    [HttpGet("product/{productId:long}")]
    public async Task<IActionResult> GetByProductId(
        long productId)
    {
        var variants =
            await _variantService.GetByProductIdAsync(
                productId
            );

        return Ok(variants);
    }


    // =====================================================
    // CREATE
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductVariantDTO request)
    {
        var variant =
            await _variantService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = variant.Id
            },
            variant
        );
    }


    // =====================================================
    // UPDATE
    // =====================================================

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateProductVariantDTO request)
    {
        var variant =
            await _variantService.UpdateAsync(
                id,
                request
            );

        if (variant == null)
        {
            return NotFound(new
            {
                message = "Product variant not found."
            });
        }

        return Ok(variant);
    }


    // =====================================================
    // DELETE
    // =====================================================

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result =
            await _variantService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Product variant not found."
            });
        }

        return NoContent();
    }
}