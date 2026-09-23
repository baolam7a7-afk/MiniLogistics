using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.ReturnRequest;
using MiniLogistics.BLL.Services.ReturnRequest;
using MiniLogistics.BLL.Exceptions;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/return-requests")]
[Authorize]
public class ReturnRequestController : ControllerBase
{
    private readonly IReturnRequestService _service;

    public ReturnRequestController(
        IReturnRequestService service)
    {
        _service = service;
    }

    // =====================================================
    // CREATE
    // CUSTOMER
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> Create(
        [FromBody] CreateReturnRequestDTO request)
    {
        var userId = GetUserId();

        var result =
            await _service.CreateAsync(
                userId,
                request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    // =====================================================
    // MY REQUESTS
    // CUSTOMER
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = GetUserId();

        var result =
            await _service.GetMyRequestsAsync(
                userId);

        return Ok(result);
    }

    // =====================================================
    // GET BY ID
    // CUSTOMER / SELLER / ADMIN
    // =====================================================

    [HttpGet("{id:long}")]
    [Authorize(Roles = "customer,seller,admin")]
    public async Task<IActionResult> GetById(
        long id)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result =
            await _service.GetByIdAsync(
                userId,
                role,
                id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    // =====================================================
    // SHOP REQUESTS
    // SELLER
    // =====================================================

    [HttpGet("shop")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> GetShopRequests()
    {
        var sellerId = GetUserId();

        var result =
            await _service.GetShopRequestsAsync(
                sellerId);

        return Ok(result);
    }

    // =====================================================
    // ALL
    // ADMIN
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _service.GetAllAsync();

        return Ok(result);
    }

    // =====================================================
    // APPROVE
    // SELLER / ADMIN
    // =====================================================

    [HttpPut("{id:long}/approve")]
    [Authorize(Roles = "seller,admin")]
    public async Task<IActionResult> Approve(
        long id)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result =
            await _service.ApproveAsync(
                userId,
                role,
                id);

        return Ok(result);
    }

    // =====================================================
    // REJECT
    // SELLER / ADMIN
    // =====================================================

    [HttpPut("{id:long}/reject")]
    [Authorize(Roles = "seller,admin")]
    public async Task<IActionResult> Reject(
        long id)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result =
            await _service.RejectAsync(
                userId,
                role,
                id);

        return Ok(result);
    }

    // =====================================================
    // USER ID
    // =====================================================

    private long GetUserId()
    {
        var claim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);

        if (claim == null)
        {
            throw new UnauthorizedException(
                "Không tìm thấy UserId trong JWT.");
        }

        if (!long.TryParse(
                claim.Value,
                out var userId))
        {
            throw new UnauthorizedException(
                "UserId trong JWT không hợp lệ.");
        }

        return userId;
    }

    // =====================================================
    // ROLE
    // =====================================================

    private string GetRole()
    {
        return User.FindFirst(
                   ClaimTypes.Role)
                   ?.Value
                   ?.Trim()
                   .ToLowerInvariant()
               ?? throw new UnauthorizedException(
                   "Không tìm thấy Role trong JWT.");
    }
}