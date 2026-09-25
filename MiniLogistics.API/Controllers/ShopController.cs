using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Shop;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.BLL.Services.Shop;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/shops")]
public class ShopController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopController(IShopService shopService)
    {
        _shopService = shopService;
    }


    // =====================================================
    // ADMIN - GET ALL SHOPS
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PagedResponseDTO<ShopResponseDTO>>>
        GetAll(
            [FromQuery] ShopPaginationRequestDTO request)
    {
        var result =
            await _shopService.GetAllAsync(request);

        return Ok(result);
    }


    // =====================================================
    // ADMIN - GET PENDING SHOPS
    // =====================================================

    [HttpGet("pending")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<ShopResponseDTO>>>
        GetPending()
    {
        var result =
            await _shopService.GetPendingAsync();

        return Ok(result);
    }


    // =====================================================
    // ADMIN - APPROVE SHOP
    // =====================================================

    [HttpPut("{id:long}/approve")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ShopResponseDTO>>
        Approve(long id)
    {
        var result =
            await _shopService.ApproveAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy Shop."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // ADMIN - REJECT SHOP
    // =====================================================

    [HttpPut("{id:long}/reject")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ShopResponseDTO>>
        Reject(long id)
    {
        var result =
            await _shopService.RejectAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy Shop."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // GET SHOP BY ID
    // PUBLIC
    // =====================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopResponseDTO>>
        GetById(long id)
    {
        var result =
            await _shopService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy Shop."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // SELLER - CREATE SHOP
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
            await _shopService.CreateAsync(
                userId,
                request);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Id
            },
            result);
    }


    // =====================================================
    // SELLER - GET MY SHOPS
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<IEnumerable<ShopResponseDTO>>>
        GetMyShops()
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService.GetMyShopsAsync(
                userId);

        return Ok(result);
    }


    // =====================================================
    // SELLER - GET MY SHOP BY ID
    // =====================================================

    [HttpGet("my/{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        GetMyShopById(long id)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService.GetMyShopByIdAsync(
                userId,
                id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy Shop hoặc Shop không thuộc quyền sở hữu của bạn."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // SELLER - UPDATE MY SHOP
    // =====================================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        Update(
            long id,
            [FromBody] UpdateShopDTO request)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService.UpdateAsync(
                userId,
                id,
                request);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy Shop hoặc Shop không thuộc quyền sở hữu của bạn."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // GET CURRENT USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                userId,
                out var parsedUserId))
        {
            throw new UnauthorizedException(
                "Token không chứa UserId hợp lệ.");
        }

        return parsedUserId;
    }
}