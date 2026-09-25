using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.PayoutRequest;
using MiniLogistics.BLL.Services.PayoutRequest;
using System.Security.Claims;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/payout-requests")]
[Authorize]
public class PayoutRequestController : ControllerBase
{
    private readonly IPayoutRequestService _payoutRequestService;

    public PayoutRequestController(
        IPayoutRequestService payoutRequestService)
    {
        _payoutRequestService = payoutRequestService;
    }

    // =====================================================
    // SELLER - CREATE PAYOUT REQUEST
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> Create(
        [FromBody] CreatePayoutRequestDTO request)
    {
        var userId = GetUserId();

        var result = await _payoutRequestService
            .CreateAsync(userId, request);

        return Ok(result);
    }


    // =====================================================
    // SELLER - GET MY PAYOUT REQUESTS
    // =====================================================

    [HttpGet("my/{shopId:long}")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> GetMy(
        long shopId,
        [FromQuery] PayoutRequestPaginationRequestDTO request)
    {
        var userId = GetUserId();

        var result = await _payoutRequestService
            .GetMyAsync(
                userId,
                shopId,
                request);

        return Ok(result);
    }


    // =====================================================
    // ADMIN - GET ALL PAYOUT REQUESTS
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] PayoutRequestPaginationRequestDTO request)
    {
        var result = await _payoutRequestService
            .GetAllAsync(request);

        return Ok(result);
    }


    // =====================================================
    // ADMIN - GET BY ID
    // =====================================================

    [HttpGet("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _payoutRequestService
            .GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = $"PayoutRequest {id} không tồn tại."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // ADMIN - APPROVE
    // =====================================================

    [HttpPut("{id:long}/approve")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Approve(long id)
    {
        var adminUserId = GetUserId();

        var result = await _payoutRequestService
            .ApproveAsync(
                adminUserId,
                id);

        return Ok(result);
    }


    // =====================================================
    // ADMIN - REJECT
    // =====================================================

    [HttpPut("{id:long}/reject")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Reject(long id)
    {
        var adminUserId = GetUserId();

        var result = await _payoutRequestService
            .RejectAsync(
                adminUserId,
                id);

        return Ok(result);
    }


    // =====================================================
    // GET USER ID FROM JWT
    // =====================================================

    private long GetUserId()
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                userIdClaim,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được User ID từ JWT.");
        }

        return userId;
    }
}