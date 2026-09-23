using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.ReportSnapshot;
using MiniLogistics.BLL.Services.ReportSnapshot;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/report-snapshots")]
public class ReportSnapshotController : ControllerBase
{
    private readonly IReportSnapshotService _service;

    public ReportSnapshotController(
        IReportSnapshotService service)
    {
        _service = service;
    }


    // =========================================================
    // POST: api/report-snapshots
    // =========================================================

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportSnapshotDTO request)
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
    // GET: api/report-snapshots/{id}
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
    // GET: api/report-snapshots
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
    // GET: api/report-snapshots/shop/{shopId}
    // =========================================================

    [HttpGet("shop/{shopId:long}")]
    [Authorize]
    public async Task<IActionResult> GetByShopId(
        long shopId)
    {
        var userId = GetUserId();

        var role = GetRole();

        var result =
            await _service.GetByShopIdAsync(
                userId,
                role,
                shopId);

        return Ok(result);
    }


    // =========================================================
    // PUT: api/report-snapshots/{id}
    // =========================================================

    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateReportSnapshotDTO request)
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
    // DELETE: api/report-snapshots/{id}
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