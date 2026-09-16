
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    // ==========================================
    // 1. TEST JWT - USER ĐÃ ĐĂNG NHẬP
    // ==========================================

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier),

            email = User.FindFirstValue(
                ClaimTypes.Email),

            roles = User.FindAll(
                ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList()
        });
    }


    // ==========================================
    // 2. CHỈ CUSTOMER
    // ==========================================

    [HttpGet("customer")]
    [Authorize(Roles = "customer")]
    public IActionResult Customer()
    {
        return Ok(new
        {
            message = "Bạn là Customer",

            userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier),

            email = User.FindFirstValue(
                ClaimTypes.Email),

            roles = User.FindAll(
                ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList()
        });
    }


    // ==========================================
    // 3. CHỈ ADMIN
    // ==========================================

    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public IActionResult Admin()
    {
        return Ok(new
        {
            message = "Bạn là Admin",

            userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier),

            email = User.FindFirstValue(
                ClaimTypes.Email),

            roles = User.FindAll(
                ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList()
        });
    }


    // ==========================================
    // 4. CHỈ SELLER
    // ==========================================

    [HttpGet("seller")]
    [Authorize(Roles = "seller")]
    public IActionResult Seller()
    {
        return Ok(new
        {
            message = "Bạn là Seller",

            userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier),

            email = User.FindFirstValue(
                ClaimTypes.Email),

            roles = User.FindAll(
                ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList()
        });
    }


    // ==========================================
    // 5. CHỈ SHIPPER
    // ==========================================

    [HttpGet("shipper")]
    [Authorize(Roles = "shipper")]
    public IActionResult Shipper()
    {
        return Ok(new
        {
            message = "Bạn là Shipper",

            userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier),

            email = User.FindFirstValue(
                ClaimTypes.Email),

            roles = User.FindAll(
                ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList()
        });
    }
}