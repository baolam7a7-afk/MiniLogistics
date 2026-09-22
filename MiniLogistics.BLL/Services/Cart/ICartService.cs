using MiniLogistics.BLL.DTOs.Cart;

namespace MiniLogistics.BLL.Services.Cart;

public interface ICartService
{
    Task<CartResponseDTO> GetMyCartAsync(long userId);

    Task<CartResponseDTO> AddToCartAsync(
        long userId,
        AddToCartDTO request);

    Task<CartResponseDTO> UpdateCartItemAsync(
        long userId,
        long cartItemId,
        UpdateCartItemDTO request);

    Task RemoveCartItemAsync(
        long userId,
        long cartItemId);

    Task ClearCartAsync(
        long userId);
}