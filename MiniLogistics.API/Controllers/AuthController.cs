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
    // GOOGLE LOGIN
    // POST: api/auth/google
    // =====================================================

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequestDTO request)
    {
        try
        {
            var result =
                await _authService.GoogleLoginAsync(request);

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
    // REFRESH TOKEN
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


    // =====================================================
    // FORGOT PASSWORD
    // POST: api/auth/forgot-password
    // =====================================================

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDTO request)
    {
        try
        {
            var resetToken =
                await _authService.ForgotPasswordAsync(request);

            return Ok(new
            {
                message = "Tạo Reset Password Token thành công.",

                // DEVELOPMENT / SWAGGER ONLY
                // Production sẽ gửi token qua Email
                resetToken = resetToken
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


    // =====================================================
    // RESET PASSWORD
    // POST: api/auth/reset-password
    // =====================================================

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDTO request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);

            return Ok(new
            {
                message = "Đặt lại mật khẩu thành công."
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
    // =====================================================
    // CHANGE PASSWORD
    // POST: api/auth/change-password
    // =====================================================

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDTO request)
    {
        try
        {
            var userIdClaim =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(new
                {
                    message = "Không xác định được User."
                });
            }

            if (!long.TryParse(
                    userIdClaim.Value,
                    out long userId))
            {
                return Unauthorized(new
                {
                    message = "UserId trong JWT không hợp lệ."
                });
            }

            await _authService.ChangePasswordAsync(
                userId,
                request);

            return Ok(new
            {
                message = "Đổi mật khẩu thành công."
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