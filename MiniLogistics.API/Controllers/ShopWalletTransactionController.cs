using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.ShopWalletTransaction;
using MiniLogistics.BLL.Services.ShopWalletTransaction;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/shop-wallet-transactions")]
[Authorize]
public class ShopWalletTransactionController
    : ControllerBase
{
    private readonly IShopWalletTransactionService
        _transactionService;

    public ShopWalletTransactionController(
        IShopWalletTransactionService transactionService)
    {
        _transactionService =
            transactionService;
    }

    // =====================================================
    // CREATE - ADMIN
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<
        ActionResult<ShopWalletTransactionResponseDTO>>
        Create(
            CreateShopWalletTransactionDTO request)
    {
        var adminUserId =
            GetCurrentUserId();

        var result =
            await _transactionService
                .CreateAsync(
                    adminUserId,
                    request);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                transactionId = result.Id
            },
            result);
    }

    // =====================================================
    // GET MY TRANSACTIONS - SELLER
    // =====================================================

    [HttpGet("my/{shopId:long}")]
    [Authorize(Roles = "seller")]
    public async Task<
        ActionResult<IEnumerable<
            ShopWalletTransactionResponseDTO>>>
        GetMyTransactions(long shopId)
    {
        var sellerId =
            GetCurrentUserId();

        var result =
            await _transactionService
                .GetMyTransactionsAsync(
                    sellerId,
                    shopId);

        return Ok(result);
    }

    // =====================================================
    // GET ALL - ADMIN
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<
        ActionResult<IEnumerable<
            ShopWalletTransactionResponseDTO>>>
        GetAll()
    {
        var result =
            await _transactionService
                .GetAllAsync();

        return Ok(result);
    }

    // =====================================================
    // GET BY ID - ADMIN
    // =====================================================

    [HttpGet("{transactionId:long}")]
    [Authorize(Roles = "admin")]
    public async Task<
        ActionResult<ShopWalletTransactionResponseDTO>>
        GetById(long transactionId)
    {
        var result =
            await _transactionService
                .GetByIdAsync(transactionId);

        if (result == null)
        {
            return NotFound(
                new
                {
                    message =
                        "Không tìm thấy Wallet Transaction."
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