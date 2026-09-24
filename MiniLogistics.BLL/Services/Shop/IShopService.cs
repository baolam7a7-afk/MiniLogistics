using MiniLogistics.BLL.DTOs.Shop;

namespace MiniLogistics.BLL.Services.Shop;

public interface IShopService
{
    // =====================================================
    // SELLER
    // =====================================================

    Task<ShopResponseDTO> CreateAsync(
        long ownerUserId,
        CreateShopDTO request);

    Task<IEnumerable<ShopResponseDTO>> GetMyShopsAsync(
        long ownerUserId);

    Task<ShopResponseDTO?> GetMyShopByIdAsync(
        long ownerUserId,
        long shopId);

    Task<ShopResponseDTO?> UpdateAsync(
        long ownerUserId,
        long shopId,
        UpdateShopDTO request);


    // =====================================================
    // COMMON
    // =====================================================

    Task<ShopResponseDTO?> GetByIdAsync(
        long shopId);


    // =====================================================
    // ADMIN - SHOP APPROVAL
    // =====================================================

    Task<IEnumerable<ShopResponseDTO>> GetPendingAsync();

    Task<ShopResponseDTO?> ApproveAsync(
        long shopId);

    Task<ShopResponseDTO?> RejectAsync(
        long shopId);
}