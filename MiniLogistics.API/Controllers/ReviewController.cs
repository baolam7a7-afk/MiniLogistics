using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.Review;
using MiniLogistics.BLL.Services.Review;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(
        IReviewService reviewService)
    {
        _reviewService = reviewService;
    }


    // =====================================================
    // CREATE REVIEW
    // CUSTOMER
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewDTO request)
    {
        long customerId = GetUserId();

        var result =
            await _reviewService.CreateAsync(
                customerId,
                request);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Id
            },
            result);
    }


    // =====================================================
    // GET REVIEW BY ID
    // PUBLIC
    // =====================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(
        long id)
    {
        var result =
            await _reviewService
                .GetByIdAsync(id);

        return Ok(result);
    }


    // =====================================================
    // GET REVIEWS BY PRODUCT
    // PUBLIC + PAGINATION
    // =====================================================

    [HttpGet("product/{productId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByProduct(
        long productId,
        [FromQuery] ReviewPaginationRequestDTO request)
    {
        var result =
            await _reviewService
                .GetByProductIdAsync(
                    productId,
                    request);

        return Ok(result);
    }


    // =====================================================
    // GET MY REVIEWS
    // CUSTOMER + PAGINATION
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] ReviewPaginationRequestDTO request)
    {
        long customerId = GetUserId();

        var result =
            await _reviewService
                .GetMyReviewsAsync(
                    customerId,
                    request);

        return Ok(result);
    }


    // =====================================================
    // UPDATE REVIEW
    // CUSTOMER
    // =====================================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateReviewDTO request)
    {
        long customerId = GetUserId();

        var result =
            await _reviewService.UpdateAsync(
                customerId,
                id,
                request);

        return Ok(result);
    }


    // =====================================================
    // DELETE REVIEW
    // CUSTOMER
    // =====================================================

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> Delete(
        long id)
    {
        long customerId = GetUserId();

        await _reviewService.DeleteAsync(
            customerId,
            id);

        return NoContent();
    }


    // =====================================================
    // GET USER ID FROM JWT
    // =====================================================

    private long GetUserId()
    {
        var claim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);

        if (claim == null)
        {
            throw new UnauthorizedAccessException(
                "Không tìm thấy UserId trong JWT.");
        }

        if (!long.TryParse(
                claim.Value,
                out long userId))
        {
            throw new UnauthorizedAccessException(
                "UserId trong JWT không hợp lệ.");
        }

        return userId;
    }
}