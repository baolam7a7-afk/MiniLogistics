using MiniLogistics.BLL.DTOs.Cart;

namespace MiniLogistics.BLL.Services.Cart;

public interface ICartService
{
    // =====================================================
    // GET MY CART
    // =====================================================

    Task<CartResponseDTO> GetMyCartAsync(
        long userId);


    // =====================================================
    // ADD TO CART
    // =====================================================

    Task<CartResponseDTO> AddToCartAsync(
        long userId,
        AddToCartDTO request);


    // =====================================================
    // UPDATE CART ITEM
    // =====================================================

    Task<CartResponseDTO> UpdateCartItemAsync(
        long userId,
        long cartItemId,
        UpdateCartItemDTO request);


    // =====================================================
    // REMOVE CART ITEM
    // =====================================================

    Task RemoveCartItemAsync(
        long userId,
        long cartItemId);


    // =====================================================
    // CLEAR CART
    // =====================================================

    Task ClearCartAsync(
        long userId);
}