using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Product;
using MiniLogistics.BLL.Exceptions;
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


    // =====================================================
    // GET ALL
    // PUBLIC
    // GET: /api/products
    // =====================================================

    [HttpGet]
    [AllowAnonymous]
    public async Task<
        ActionResult<PagedResponseDTO<ProductResponseDTO>>>
        GetAll(
            [FromQuery] ProductPaginationRequestDTO request)
    {
        var products =
            await _productService
                .GetAllAsync(request);

        return Ok(products);
    }


    // =====================================================
    // GET BY ID
    // PUBLIC
    // GET: /api/products/{id}
    // =====================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<
        ActionResult<ProductResponseDTO>>
        GetById(
            long id)
    {
        var product =
            await _productService
                .GetByIdAsync(id);

        if (product == null)
        {
            return NotFound(new
            {
                message =
                    "Product không tồn tại."
            });
        }

        return Ok(product);
    }


    // =====================================================
    // GET BY CATEGORY
    // PUBLIC
    // GET: /api/products/category/{categoryId}
    // =====================================================

    [HttpGet("category/{categoryId:long}")]
    [AllowAnonymous]
    public async Task<
        ActionResult<IEnumerable<ProductResponseDTO>>>
        GetByCategory(
            long categoryId)
    {
        var products =
            await _productService
                .GetByCategoryAsync(
                    categoryId);

        return Ok(products);
    }


    // =====================================================
    // SEARCH
    // PUBLIC
    // GET: /api/products/search?keyword=...
    // =====================================================

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<
        ActionResult<IEnumerable<ProductResponseDTO>>>
        Search(
            [FromQuery] string keyword)
    {
        var products =
            await _productService
                .SearchAsync(keyword);

        return Ok(products);
    }


    // =====================================================
    // CREATE
    // SELLER ONLY
    // POST: /api/products
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "seller")]
    public async Task<
        ActionResult<ProductResponseDTO>>
        Create(
            [FromBody] CreateProductDTO request)
    {
        var userId =
            GetCurrentUserId();

        var product =
            await _productService
                .CreateAsync(
                    userId,
                    request);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = product.Id
            },
            product);
    }


    // =====================================================
    // UPDATE
    // SELLER ONLY
    // PUT: /api/products/{id}
    // =====================================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<
        ActionResult<ProductResponseDTO>>
        Update(
            long id,
            [FromBody] UpdateProductDTO request)
    {
        var userId =
            GetCurrentUserId();

        var product =
            await _productService
                .UpdateAsync(
                    userId,
                    id,
                    request);

        if (product == null)
        {
            return NotFound(new
            {
                message =
                    "Product không tồn tại."
            });
        }

        return Ok(product);
    }


    // =====================================================
    // DELETE
    // SELLER ONLY
    // DELETE: /api/products/{id}
    // =====================================================

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult>
        Delete(
            long id)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _productService
                .DeleteAsync(
                    userId,
                    id);

        if (!result)
        {
            return NotFound(new
            {
                message =
                    "Product không tồn tại."
            });
        }

        return NoContent();
    }


    // =====================================================
    // GET CURRENT USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            throw new UnauthorizedException(
                "User ID không tồn tại trong JWT.");
        }

        if (!long.TryParse(
                userId,
                out var parsedUserId))
        {
            throw new UnauthorizedException(
                "User ID trong JWT không hợp lệ.");
        }

        return parsedUserId;
    }
}