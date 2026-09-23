using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Payment;
using MiniLogistics.BLL.Services.Payment;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(
        IPaymentService paymentService)
    {
        _paymentService =
            paymentService;
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
            await _paymentService
                .GetAllAsync();

        return Ok(result);
    }


    // =====================================================
    // GET MY PAYMENTS
    // CUSTOMER
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> GetMy()
    {
        long customerId =
            GetCurrentUserId();

        var result =
            await _paymentService
                .GetMyPaymentsAsync(
                    customerId);

        return Ok(result);
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    [HttpGet("{id:long}")]
    [Authorize(
        Roles = "customer,seller,admin")]
    public async Task<IActionResult> GetById(
        long id)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _paymentService
                .GetByIdAsync(
                    id,
                    userId,
                    role);

        return Ok(result);
    }


    // =====================================================
    // GET BY ORDER
    // =====================================================

    [HttpGet("order/{orderId:long}")]
    [Authorize(
        Roles = "customer,seller,admin")]
    public async Task<IActionResult> GetByOrder(
        long orderId)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _paymentService
                .GetByOrderIdAsync(
                    orderId,
                    userId,
                    role);

        return Ok(result);
    }


    // =====================================================
    // UPDATE STATUS
    // ADMIN
    // =====================================================

    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdatePaymentStatusDTO request)
    {
        var result =
            await _paymentService
                .UpdateStatusAsync(
                    id,
                    request);

        return Ok(result);
    }


    // =====================================================
    // CURRENT USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                value,
                out long userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được User ID.");
        }

        return userId;
    }


    // =====================================================
    // CURRENT ROLE
    // =====================================================

    private string GetCurrentRole()
    {
        var role =
            User.FindFirstValue(
                ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được Role.");
        }

        return role;
    }
}