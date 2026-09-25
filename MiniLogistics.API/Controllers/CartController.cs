using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Cart;
using MiniLogistics.BLL.Services.Cart;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(
        ICartService cartService)
    {
        _cartService = cartService;
    }


    // =====================================================
    // GET MY CART
    // GET: api/cart
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetMyCart()
    {
        var userId = GetCurrentUserId();

        var result =
            await _cartService.GetMyCartAsync(userId);

        return Ok(result);
    }


    // =====================================================
    // ADD TO CART
    // POST: api/cart/items
    // =====================================================

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart(
        [FromBody] AddToCartDTO request)
    {
        var userId = GetCurrentUserId();

        var result =
            await _cartService.AddToCartAsync(
                userId,
                request);

        return Ok(result);
    }


    // =====================================================
    // UPDATE CART ITEM
    // PUT: api/cart/items/{cartItemId}
    // =====================================================

    [HttpPut("items/{cartItemId:long}")]
    public async Task<IActionResult> UpdateCartItem(
        long cartItemId,
        [FromBody] UpdateCartItemDTO request)
    {
        var userId = GetCurrentUserId();

        var result =
            await _cartService.UpdateCartItemAsync(
                userId,
                cartItemId,
                request);

        return Ok(result);
    }


    // =====================================================
    // REMOVE CART ITEM
    // DELETE: api/cart/items/{cartItemId}
    // =====================================================

    [HttpDelete("items/{cartItemId:long}")]
    public async Task<IActionResult> RemoveCartItem(
        long cartItemId)
    {
        var userId = GetCurrentUserId();

        await _cartService.RemoveCartItemAsync(
            userId,
            cartItemId);

        return Ok(new
        {
            message =
                "Đã xóa sản phẩm khỏi giỏ hàng."
        });
    }


    // =====================================================
    // CLEAR CART
    // DELETE: api/cart
    // =====================================================

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();

        await _cartService.ClearCartAsync(
            userId);

        return Ok(new
        {
            message =
                "Đã xóa toàn bộ giỏ hàng."
        });
    }


    // =====================================================
    // GET CURRENT USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
            userIdValue))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được UserId từ JWT.");
        }

        if (!long.TryParse(
            userIdValue,
            out var userId))
        {
            throw new UnauthorizedAccessException(
                "UserId trong JWT không hợp lệ.");
        }

        return userId;
    }
}