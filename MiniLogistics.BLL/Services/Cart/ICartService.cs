using MiniLogistics.BLL.DTOs.Cart;

namespace MiniLogistics.BLL.Services.Cart;

public interface ICartService
{
    // Lấy giỏ hàng của user hiện tại
    Task<CartResponseDto> GetCartAsync(long userId);

    // Thêm sản phẩm vào giỏ
    Task<CartResponseDto> AddToCartAsync(
        long userId,
        AddToCartDto dto
    );

    // Cập nhật số lượng CartItem
    Task<CartResponseDto> UpdateCartItemAsync(
        long userId,
        long cartItemId,
        UpdateCartItemDto dto
    );

    // Xóa một CartItem
    Task<CartResponseDto> RemoveCartItemAsync(
        long userId,
        long cartItemId
    );

    // Xóa toàn bộ Cart
    Task ClearCartAsync(long userId);
}