using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.RefundTransaction;
using MiniLogistics.BLL.Services.RefundTransaction;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/refunds")]
[Authorize]
public class RefundTransactionsController
    : ControllerBase
{
    private readonly IRefundTransactionService
        _service;

    public RefundTransactionsController(
        IRefundTransactionService service)
    {
        _service = service;
    }


    // =====================================================
    // CREATE
    // ADMIN ONLY
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create(
        [FromBody]
        CreateRefundTransactionDTO request)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _service.CreateAsync(
                userId,
                request);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }


    // =====================================================
    // GET MY REFUNDS
    // CUSTOMER
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> GetMyRefunds()
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _service
                .GetMyRefundsAsync(userId);

        return Ok(result);
    }


    // =====================================================
    // GET ALL
    // ADMIN
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _service
                .GetAllAsync();

        return Ok(result);
    }


    // =====================================================
    // GET BY ID
    // CUSTOMER / ADMIN
    // =====================================================

    [HttpGet("{id:long}")]
    [Authorize(Roles = "customer,admin")]
    public async Task<IActionResult> GetById(
        long id)
    {
        var userId =
            GetCurrentUserId();

        var role =
            User.FindFirstValue(
                ClaimTypes.Role);

        var result =
            await _service.GetByIdAsync(
                userId,
                role ?? string.Empty,
                id);

        return Ok(result);
    }


    // =====================================================
    // COMPLETE
    // ADMIN
    // =====================================================

    [HttpPut("{id:long}/complete")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Complete(
        long id)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _service.CompleteAsync(
                userId,
                id);

        return Ok(result);
    }


    // =====================================================
    // FAIL
    // ADMIN
    // =====================================================

    [HttpPut("{id:long}/fail")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Fail(
        long id)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _service.FailAsync(
                userId,
                id);

        return Ok(result);
    }


    // =====================================================
    // GET USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var claim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                claim,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được UserId.");
        }

        return userId;
    }
}