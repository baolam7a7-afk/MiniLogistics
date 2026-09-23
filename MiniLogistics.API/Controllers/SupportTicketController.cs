using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.SupportTicket;
using MiniLogistics.BLL.Services.SupportTicket;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/support-tickets")]
public class SupportTicketController : ControllerBase
{
    private readonly ISupportTicketService _service;

    public SupportTicketController(
        ISupportTicketService service)
    {
        _service = service;
    }


    // =========================================================
    // POST: api/support-tickets
    // =========================================================

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateSupportTicketDTO request)
    {
        var userId = GetUserId();

        var result =
            await _service.CreateAsync(
                userId,
                request);

        return Ok(result);
    }


    // =========================================================
    // GET: api/support-tickets/{id}
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
    // GET: api/support-tickets/my
    // =========================================================

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyTickets()
    {
        var userId = GetUserId();

        var result =
            await _service.GetMyTicketsAsync(userId);

        return Ok(result);
    }


    // =========================================================
    // GET: api/support-tickets
    // ADMIN ONLY
    // =========================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _service.GetAllAsync();

        return Ok(result);
    }


    // =========================================================
    // PUT: api/support-tickets/{id}
    // =========================================================

    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateSupportTicketDTO request)
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
    // DELETE: api/support-tickets/{id}
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