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

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }


    // =====================================================
    // GET CART
    // =====================================================

    [HttpGet]
    public async Task<ActionResult<CartResponseDto>> GetCart()
    {
        var userId = GetUserId();

        var result =
            await _cartService.GetCartAsync(userId);

        return Ok(result);
    }


    // =====================================================
    // ADD TO CART
    // =====================================================

    [HttpPost("items")]
    public async Task<ActionResult<CartResponseDto>> AddToCart(
        [FromBody] AddToCartDto request)
    {
        var userId = GetUserId();

        var result =
            await _cartService.AddToCartAsync(
                userId,
                request
            );

        return Ok(result);
    }


    // =====================================================
    // UPDATE CART ITEM
    // =====================================================

    [HttpPut("items/{cartItemId:long}")]
    public async Task<ActionResult<CartResponseDto>>
        UpdateCartItem(
            long cartItemId,
            [FromBody] UpdateCartItemDto request)
    {
        var userId = GetUserId();

        var result =
            await _cartService.UpdateCartItemAsync(
                userId,
                cartItemId,
                request
            );

        return Ok(result);
    }


    // =====================================================
    // REMOVE CART ITEM
    // =====================================================

    [HttpDelete("items/{cartItemId:long}")]
    public async Task<ActionResult<CartResponseDto>>
        RemoveCartItem(long cartItemId)
    {
        var userId = GetUserId();

        var result =
            await _cartService.RemoveCartItemAsync(
                userId,
                cartItemId
            );

        return Ok(result);
    }


    // =====================================================
    // CLEAR CART
    // =====================================================

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();

        await _cartService.ClearCartAsync(userId);

        return NoContent();
    }


    // =====================================================
    // GET USER ID FROM JWT
    // =====================================================

    private long GetUserId()
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được UserId từ JWT."
            );
        }

        return userId;
    }
}