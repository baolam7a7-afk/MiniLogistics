using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Shop;
using MiniLogistics.BLL.Services.Shop;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/shops")]
[Authorize(Roles = "seller")]
public class ShopController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopController(
        IShopService shopService)
    {
        _shopService = shopService;
    }


    // =====================================================
    // CREATE SHOP
    // POST: /api/shops
    // =====================================================

    [HttpPost]
    public async Task<ActionResult<ShopResponseDto>> CreateShop(
        [FromBody] CreateShopDto request)
    {
        var userId = GetUserId();

        var result =
            await _shopService.CreateShopAsync(
                userId,
                request
            );

        return Ok(result);
    }


    // =====================================================
    // GET MY SHOP
    // GET: /api/shops/me
    // =====================================================

    [HttpGet("me")]
    public async Task<ActionResult<ShopResponseDto>> GetMyShop()
    {
        var userId = GetUserId();

        var result =
            await _shopService.GetMyShopAsync(
                userId
            );

        return Ok(result);
    }


    // =====================================================
    // GET USER ID FROM JWT
    // =====================================================

    private long GetUserId()
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (!long.TryParse(
                userIdClaim,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được UserId từ JWT."
            );
        }

        return userId;
    }
}