using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Category;
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
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories =
            await _categoryService.GetAllAsync();

        return Ok(categories);
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id)
    {
        var category =
            await _categoryService.GetByIdAsync(id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category không tồn tại."
            });
        }

        return Ok(category);
    }


    // =====================================================
    // CREATE
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryDTO request)
    {
        var category =
            await _categoryService.CreateAsync(
                request
            );

        return CreatedAtAction(
            nameof(GetById),

            new
            {
                id = category.Id
            },

            category
        );
    }


    // =====================================================
    // UPDATE
    // =====================================================

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateCategoryDTO request)
    {
        var category =
            await _categoryService.UpdateAsync(
                id,
                request
            );

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category không tồn tại."
            });
        }

        return Ok(category);
    }


    // =====================================================
    // DELETE
    // =====================================================

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id)
    {
        var result =
            await _categoryService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Category không tồn tại."
            });
        }

        return NoContent();
    }
}