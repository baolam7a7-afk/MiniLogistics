using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.SupportMessage;
using MiniLogistics.BLL.Services.SupportMessage;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/support-messages")]
public class SupportMessageController : ControllerBase
{
    private readonly ISupportMessageService _service;

    public SupportMessageController(
        ISupportMessageService service)
    {
        _service = service;
    }


    // =========================================================
    // POST: api/support-messages
    // =========================================================

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateSupportMessageDTO request)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result =
            await _service.CreateAsync(
                userId,
                role,
                request);

        return Ok(result);
    }


    // =========================================================
    // GET: api/support-messages/{id}
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
    // GET:
    // api/support-messages/ticket/{ticketId}
    // PAGINATION
    // =========================================================

    [HttpGet("ticket/{ticketId:long}")]
    [Authorize]
    public async Task<
        ActionResult<PagedResponseDTO<SupportMessageResponseDTO>>>
        GetByTicketId(
            long ticketId,
            [FromQuery] SupportMessagePaginationRequestDTO request)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result =
            await _service.GetByTicketIdAsync(
                userId,
                role,
                ticketId,
                request);

        return Ok(result);
    }


    // =========================================================
    // PUT: api/support-messages/{id}
    // =========================================================

    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateSupportMessageDTO request)
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
    // DELETE: api/support-messages/{id}
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