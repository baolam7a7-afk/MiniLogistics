using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Auth;
using MiniLogistics.BLL.Services.Auth;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // =====================================================
    // REGISTER
    // POST: api/auth/register
    // =====================================================
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDTO request)
    {
        try
        {
            var result =
                await _authService.RegisterAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // LOGIN
    // POST: api/auth/login
    // =====================================================
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDTO request)
    {
        try
        {
            var result =
                await _authService.LoginAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // REFRESH
    // POST: api/auth/refresh
    // =====================================================
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequestDTO request)
    {
        try
        {
            var result =
                await _authService.RefreshTokenAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // LOGOUT
    // POST: api/auth/logout
    // =====================================================
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDTO request)
    {
        try
        {
            await _authService.LogoutAsync(request);

            return Ok(new
            {
                message = "Đăng xuất thành công."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}