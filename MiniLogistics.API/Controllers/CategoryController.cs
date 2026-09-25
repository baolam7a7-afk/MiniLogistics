using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Category;
using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.Services;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(
        ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }


    // =====================================================
    // GET ALL
    // GET: /api/categories
    // =====================================================

    [HttpGet]
    [AllowAnonymous]
    public async Task<
        ActionResult<PagedResponseDTO<CategoryResponseDTO>>>
        GetAll(
            [FromQuery] CategoryPaginationRequestDTO request)
    {
        var categories =
            await _categoryService
                .GetAllAsync(request);

        return Ok(categories);
    }


    // =====================================================
    // GET BY ID
    // GET: /api/categories/{id}
    // =====================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<
        ActionResult<CategoryResponseDTO>>
        GetById(
            long id)
    {
        var category =
            await _categoryService
                .GetByIdAsync(id);

        if (category == null)
        {
            return NotFound(new
            {
                message =
                    "Category không tồn tại."
            });
        }

        return Ok(category);
    }


    // =====================================================
    // CREATE
    // ADMIN ONLY
    // POST: /api/categories
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<
        ActionResult<CategoryResponseDTO>>
        Create(
            [FromBody] CreateCategoryDTO request)
    {
        var category =
            await _categoryService
                .CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),

            new
            {
                id = category.Id
            },

            category);
    }


    // =====================================================
    // UPDATE
    // ADMIN ONLY
    // PUT: /api/categories/{id}
    // =====================================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<
        ActionResult<CategoryResponseDTO>>
        Update(
            long id,
            [FromBody] UpdateCategoryDTO request)
    {
        var category =
            await _categoryService
                .UpdateAsync(
                    id,
                    request);

        if (category == null)
        {
            return NotFound(new
            {
                message =
                    "Category không tồn tại."
            });
        }

        return Ok(category);
    }


    // =====================================================
    // DELETE
    // ADMIN ONLY
    // DELETE: /api/categories/{id}
    // =====================================================

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult>
        Delete(
            long id)
    {
        var result =
            await _categoryService
                .DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message =
                    "Category không tồn tại."
            });
        }

        return NoContent();
    }
}