using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.User;
using MiniLogistics.BLL.Services.User;
using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }


    // =========================================================
    // PROFILE - CURRENT USER
    // =========================================================

    /// <summary>
    /// Lấy thông tin Profile của user đang đăng nhập.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();

        var result = await _userService.GetMyProfileAsync(userId);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy thông tin user."
            });
        }

        return Ok(result);
    }


    /// <summary>
    /// Cập nhật Profile của user đang đăng nhập.
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileDTO request)
    {
        var userId = GetCurrentUserId();

        var result = await _userService.UpdateMyProfileAsync(
            userId,
            request);

        return Ok(result);
    }


    // =========================================================
    // ADMIN - USER MANAGEMENT
    // =========================================================

    /// <summary>
    /// Lấy danh sách user.
    /// Chỉ Admin được phép sử dụng.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] UserPaginationRequestDTO request)
    {
        var result = await _userService.GetAllAsync(request);

        return Ok(result);
    }


    /// <summary>
    /// Lấy user theo ID.
    /// Chỉ Admin được phép sử dụng.
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _userService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = $"User {id} không tồn tại."
            });
        }

        return Ok(result);
    }


    /// <summary>
    /// Khóa user.
    /// Chỉ Admin được phép sử dụng.
    /// </summary>
    [HttpPut("{id:long}/lock")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Lock(long id)
    {
        var result = await _userService.LockAsync(id);

        return Ok(result);
    }


    /// <summary>
    /// Mở khóa user.
    /// Chỉ Admin được phép sử dụng.
    /// </summary>
    [HttpPut("{id:long}/unlock")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Unlock(long id)
    {
        var result = await _userService.UnlockAsync(id);

        return Ok(result);
    }


    /// <summary>
    /// Thay đổi Role của user.
    /// Chỉ Admin được phép sử dụng.
    /// </summary>
    [HttpPut("{id:long}/role")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateRole(
        long id,
        [FromBody] UpdateUserRoleDTO request)
    {
        var result = await _userService.UpdateRoleAsync(
            id,
            request);

        return Ok(result);
    }


    // =========================================================
    // HELPER
    // =========================================================

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            throw new UnauthorizedAccessException(
                "Không tìm thấy UserId trong JWT.");
        }

        if (!long.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException(
                "UserId trong JWT không hợp lệ.");
        }

        if (userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "UserId không hợp lệ.");
        }

        return userId;
    }
}