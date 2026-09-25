using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Voucher;
using MiniLogistics.BLL.Services.Voucher;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/vouchers")]
[Authorize]
public class VoucherController : ControllerBase
{
    private readonly IVoucherService _service;

    public VoucherController(
        IVoucherService service)
    {
        _service = service;
    }


    // =====================================================
    // GET ALL
    // PUBLIC
    // =====================================================

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] VoucherPaginationRequestDTO request)
    {
        var result =
            await _service.GetAllAsync(request);

        return Ok(result);
    }


    // =====================================================
    // GET BY ID
    // PUBLIC
    // =====================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(
        long id)
    {
        var result =
            await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }


    // =====================================================
    // GET BY SHOP
    // PUBLIC
    // =====================================================

    [HttpGet("shop/{shopId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByShop(
        long shopId,
        [FromQuery] VoucherPaginationRequestDTO request)
    {
        var result =
            await _service.GetByShopIdAsync(
                shopId,
                request);

        return Ok(result);
    }


    // =====================================================
    // GET BY CODE
    // PUBLIC
    // =====================================================

    [HttpGet("code/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCode(
        string code)
    {
        var result =
            await _service.GetByCodeAsync(code);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }


    // =====================================================
    // VALIDATE
    // PUBLIC
    // =====================================================

    [HttpGet("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate(
        [FromQuery] string code,
        [FromQuery] decimal orderValue,
        [FromQuery] long? shopId)
    {
        var result =
            await _service.ValidateAsync(
                code,
                orderValue,
                shopId);

        return Ok(result);
    }


    // =====================================================
    // CREATE
    // ADMIN / SELLER
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Create(
        [FromBody] CreateVoucherDTO request)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _service.CreateAsync(
                userId,
                role,
                request);

        return Ok(result);
    }


    // =====================================================
    // UPDATE
    // ADMIN / SELLER
    // =====================================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateVoucherDTO request)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _service.UpdateAsync(
                id,
                userId,
                role,
                request);

        return Ok(result);
    }


    // =====================================================
    // DELETE
    // ADMIN / SELLER
    // =====================================================

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Delete(
        long id)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        await _service.DeleteAsync(
            id,
            userId,
            role);

        return NoContent();
    }


    // =====================================================
    // CLAIMS
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