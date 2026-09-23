using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.ShopWallet;
using MiniLogistics.BLL.Services.ShopWallet;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/shop-wallets")]
[Authorize]
public class ShopWalletController : ControllerBase
{
    private readonly IShopWalletService _shopWalletService;

    public ShopWalletController(
        IShopWalletService shopWalletService)
    {
        _shopWalletService = shopWalletService;
    }


    // =====================================================
    // SELLER - CREATE WALLET
    // =====================================================

    [HttpPost("{shopId:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopWalletResponseDTO>> Create(
        long shopId)
    {
        var userId = GetCurrentUserId();

        var result =
            await _shopWalletService.CreateAsync(
                userId,
                shopId);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                walletId = result.Id
            },
            result);
    }


    // =====================================================
    // SELLER - GET MY WALLET
    // =====================================================

    [HttpGet("my/{shopId:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopWalletResponseDTO>>
        GetMyWallet(long shopId)
    {
        var userId = GetCurrentUserId();

        var result =
            await _shopWalletService.GetMyWalletAsync(
                userId,
                shopId);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Shop này chưa có Wallet."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // ADMIN - GET WALLET
    // =====================================================

    [HttpGet("{walletId:long}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ShopWalletResponseDTO>>
        GetById(long walletId)
    {
        var result =
            await _shopWalletService.GetByIdAsync(
                walletId);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy Wallet."
            });
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
            throw new UnauthorizedAccessException(
                "Không tìm thấy UserId trong JWT.");
        }

        if (!long.TryParse(
                claim.Value,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "UserId trong JWT không hợp lệ.");
        }

        return userId;
    }
}