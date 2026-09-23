using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.ReviewReply;
using MiniLogistics.BLL.Services.ReviewReply;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/review-replies")]
public class ReviewReplyController : ControllerBase
{
    private readonly IReviewReplyService _service;

    public ReviewReplyController(
        IReviewReplyService service)
    {
        _service = service;
    }

    // =========================================================
    // POST: api/review-replies
    // =========================================================

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewReplyDTO request)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result = await _service.CreateAsync(
            userId,
            role,
            request);

        return Ok(result);
    }

    // =========================================================
    // GET: api/review-replies/{id}
    // =========================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(
        long id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // =========================================================
    // GET: api/review-replies/review/{reviewId}
    // =========================================================

    [HttpGet("review/{reviewId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByReviewId(
        long reviewId)
    {
        var result =
            await _service.GetByReviewIdAsync(reviewId);

        return Ok(result);
    }

    // =========================================================
    // GET: api/review-replies/shop/{shopId}
    // =========================================================

    [HttpGet("shop/{shopId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByShopId(
        long shopId)
    {
        var result =
            await _service.GetByShopIdAsync(shopId);

        return Ok(result);
    }

    // =========================================================
    // PUT: api/review-replies/{id}
    // =========================================================

    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateReviewReplyDTO request)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result = await _service.UpdateAsync(
            userId,
            role,
            id,
            request);

        return Ok(result);
    }

    // =========================================================
    // DELETE: api/review-replies/{id}
    // =========================================================

    [HttpDelete("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Delete(
        long id)
    {
        var userId = GetUserId();

        var role = GetRole();

        await _service.DeleteAsync(
            userId,
            role,
            id);

        return NoContent();
    }

    // =========================================================
    // HELPER: GET USER ID FROM JWT
    // =========================================================

    private long GetUserId()
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("userId");

        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException(
                "Không tìm thấy UserId trong JWT.");
        }

        if (!long.TryParse(
                userIdClaim.Value,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "UserId trong JWT không hợp lệ.");
        }

        return userId;
    }

    // =========================================================
    // HELPER: GET ROLE FROM JWT
    // =========================================================

    private string GetRole()
    {
        var roleClaim =
            User.FindFirst(ClaimTypes.Role)
            ?? User.FindFirst("role");

        if (roleClaim == null)
        {
            throw new UnauthorizedAccessException(
                "Không tìm thấy Role trong JWT.");
        }

        return roleClaim.Value;
    }
}