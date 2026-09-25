using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Dispute;
using MiniLogistics.BLL.Services.Dispute;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/disputes")]
public class DisputeController : ControllerBase
{
    private readonly IDisputeService _service;

    public DisputeController(IDisputeService service)
    {
        _service = service;
    }


    // =========================================================
    // POST: api/disputes
    // =========================================================

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateDisputeDTO request)
    {
        var userId = GetUserId();

        var result =
            await _service.CreateAsync(
                userId,
                request);

        return Ok(result);
    }


    // =========================================================
    // GET: api/disputes/{id}
    // =========================================================

    [HttpGet("{id:long}")]
    [Authorize]
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


    // =========================================================
    // GET: api/disputes/my
    // PAGINATION
    // =========================================================

    [HttpGet("my")]
    [Authorize]
    public async Task<
        ActionResult<PagedResponseDTO<DisputeResponseDTO>>>
        GetMyDisputes(
            [FromQuery] DisputePaginationRequestDTO request)
    {
        var userId = GetUserId();

        var result =
            await _service.GetMyDisputesAsync(
                userId,
                request);

        return Ok(result);
    }


    // =========================================================
    // GET: api/disputes
    // ADMIN ONLY
    // PAGINATION
    // =========================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<
        ActionResult<PagedResponseDTO<DisputeResponseDTO>>>
        GetAll(
            [FromQuery] DisputePaginationRequestDTO request)
    {
        var result =
            await _service.GetAllAsync(
                request);

        return Ok(result);
    }


    // =========================================================
    // PUT: api/disputes/{id}
    // =========================================================

    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateDisputeDTO request)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result =
            await _service.UpdateAsync(
                userId,
                role,
                id,
                request);

        return Ok(result);
    }


    // =========================================================
    // DELETE: api/disputes/{id}
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
    // GET USER ID FROM JWT
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
    // GET ROLE FROM JWT
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