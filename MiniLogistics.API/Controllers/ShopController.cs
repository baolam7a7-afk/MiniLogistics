using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Shop;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.BLL.Services.Shop;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/shops")]
public class ShopController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopController(
        IShopService shopService)
    {
        _shopService =
            shopService;
    }


    // =====================================================
    // CREATE SHOP
    // SELLER ONLY
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        Create(
            [FromBody] CreateShopDTO request)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService
                .CreateAsync(
                    userId,
                    request);

        return Ok(result);
    }


    // =====================================================
    // GET MY SHOPS
    // SELLER ONLY
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "seller")]
    public async Task<
        ActionResult<IEnumerable<ShopResponseDTO>>>
        GetMyShops()
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService
                .GetMyShopsAsync(
                    userId);

        return Ok(result);
    }


    // =====================================================
    // GET MY SHOP BY ID
    // SELLER ONLY
    // =====================================================

    [HttpGet("my/{shopId:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        GetMyShopById(
            long shopId)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService
                .GetMyShopByIdAsync(
                    userId,
                    shopId);

        if (result == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại hoặc không thuộc Seller này.");
        }

        return Ok(result);
    }


    // =====================================================
    // GET SHOP BY ID
    // COMMON
    // =====================================================

    [HttpGet("{shopId:long}")]
    [Authorize]
    public async Task<ActionResult<ShopResponseDTO>>
        GetById(
            long shopId)
    {
        var result =
            await _shopService
                .GetByIdAsync(
                    shopId);

        if (result == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        return Ok(result);
    }


    // =====================================================
    // UPDATE SHOP
    // SELLER ONLY
    // =====================================================

    [HttpPut("{shopId:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        Update(
            long shopId,
            [FromBody] UpdateShopDTO request)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService
                .UpdateAsync(
                    userId,
                    shopId,
                    request);

        if (result == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại hoặc không thuộc Seller này.");
        }

        return Ok(result);
    }


    // =====================================================
    // ADMIN - GET PENDING SHOPS
    // =====================================================

    [HttpGet("admin/pending")]
    [Authorize(Roles = "admin")]
    public async Task<
        ActionResult<IEnumerable<ShopResponseDTO>>>
        GetPending()
    {
        var result =
            await _shopService
                .GetPendingAsync();

        return Ok(result);
    }


    // =====================================================
    // ADMIN - APPROVE SHOP
    // =====================================================

    [HttpPost("admin/approve/{shopId:long}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ShopResponseDTO>>
        Approve(
            long shopId)
    {
        var result =
            await _shopService
                .ApproveAsync(
                    shopId);

        if (result == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        return Ok(result);
    }


    // =====================================================
    // ADMIN - REJECT SHOP
    // =====================================================

    [HttpPost("admin/reject/{shopId:long}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ShopResponseDTO>>
        Reject(
            long shopId)
    {
        var result =
            await _shopService
                .RejectAsync(
                    shopId);

        if (result == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        return Ok(result);
    }


    // =====================================================
    // GET CURRENT USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var claim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);

        if (claim == null)
        {
            throw new UnauthorizedException(
                "Không xác định được User.");
        }

        if (!long.TryParse(
                claim.Value,
                out var userId))
        {
            throw new UnauthorizedException(
                "UserId không hợp lệ.");
        }

        return userId;
    }
}