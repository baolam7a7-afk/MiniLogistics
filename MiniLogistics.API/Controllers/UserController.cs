using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.User;
using MiniLogistics.BLL.Services.User;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "admin")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public UserController(
        IUserService userService)
    {
        _userService = userService;
    }


    // =====================================================
    // GET ALL USERS - PAGINATION
    //
    // GET:
    // /api/users?page=1&pageSize=10
    //
    // Có thể filter:
    // /api/users?page=1&pageSize=10&search=customer
    //
    // /api/users?page=1&pageSize=10&role=seller
    //
    // /api/users?page=1&pageSize=10&status=active
    //
    // /api/users?page=1&pageSize=10
    //     &search=customer
    //     &role=customer
    //     &status=active
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] UserPaginationRequestDTO request)
    {
        var result =
            await _userService.GetAllAsync(
                request);

        return Ok(result);
    }


    // =====================================================
    // GET USER BY ID
    //
    // GET:
    // /api/users/{id}
    // =====================================================

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id)
    {
        var result =
            await _userService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }


    // =====================================================
    // LOCK USER
    //
    // PUT:
    // /api/users/{id}/lock
    // =====================================================

    [HttpPut("{id:long}/lock")]
    public async Task<IActionResult> Lock(
        long id)
    {
        var result =
            await _userService.LockAsync(id);

        return Ok(result);
    }


    // =====================================================
    // UNLOCK USER
    //
    // PUT:
    // /api/users/{id}/unlock
    // =====================================================

    [HttpPut("{id:long}/unlock")]
    public async Task<IActionResult> Unlock(
        long id)
    {
        var result =
            await _userService.UnlockAsync(id);

        return Ok(result);
    }


    // =====================================================
    // UPDATE ROLE
    //
    // PUT:
    // /api/users/{id}/role
    // =====================================================

    [HttpPut("{id:long}/role")]
    public async Task<IActionResult> UpdateRole(
        long id,
        [FromBody] UpdateUserRoleDTO request)
    {
        var result =
            await _userService.UpdateRoleAsync(
                id,
                request);

        return Ok(result);
    }
}