using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.OrderVoucher;
using MiniLogistics.BLL.Services.OrderVoucher;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/order-vouchers")]
[Authorize(Roles = "customer")]
public class OrderVoucherController : ControllerBase
{
    private readonly IOrderVoucherService _service;

    public OrderVoucherController(
        IOrderVoucherService service)
    {
        _service = service;
    }

    // =====================================================
    // APPLY
    // =====================================================

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(
        [FromBody] ApplyVoucherDTO request)
    {
        var customerId =
            GetCurrentUserId();

        var result =
            await _service.ApplyAsync(
                customerId,
                request);

        return Ok(result);
    }

    // =====================================================
    // GET BY ORDER
    // =====================================================

    [HttpGet("order/{orderId:long}")]
    public async Task<IActionResult> GetByOrder(
        long orderId)
    {
        var customerId =
            GetCurrentUserId();

        var result =
            await _service.GetByOrderIdAsync(
                customerId,
                orderId);

        return Ok(result);
    }

    // =====================================================
    // REMOVE
    // =====================================================

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Remove(
        long id)
    {
        var customerId =
            GetCurrentUserId();

        await _service.RemoveAsync(
            customerId,
            id);

        return NoContent();
    }

    // =====================================================
    // GET USER ID
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
}