using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.DisputeMessage;
using MiniLogistics.BLL.Services.DisputeMessage;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/dispute-messages")]
public class DisputeMessageController : ControllerBase
{
    private readonly IDisputeMessageService _service;

    public DisputeMessageController(
        IDisputeMessageService service)
    {
        _service = service;
    }


    // =========================================================
    // POST: api/dispute-messages
    // =========================================================

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateDisputeMessageDTO request)
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
    // GET: api/dispute-messages/{id}
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
    // GET: api/dispute-messages/dispute/{disputeId}
    // =========================================================

    [HttpGet("dispute/{disputeId:long}")]
    [Authorize]
    public async Task<IActionResult> GetByDisputeId(
        long disputeId)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result =
            await _service.GetByDisputeIdAsync(
                userId,
                role,
                disputeId);

        return Ok(result);
    }


    // =========================================================
    // PUT: api/dispute-messages/{id}
    // =========================================================

    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateDisputeMessageDTO request)
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
    // DELETE: api/dispute-messages/{id}
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