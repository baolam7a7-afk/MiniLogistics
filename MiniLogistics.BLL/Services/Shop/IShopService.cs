using MiniLogistics.BLL.DTOs.Shop;

namespace MiniLogistics.BLL.Services.Shop;

public interface IShopService
{
    Task<ShopResponseDTO> CreateAsync(
        long ownerUserId,
        CreateShopDTO request);

    Task<IEnumerable<ShopResponseDTO>> GetMyShopsAsync(
        long ownerUserId);

    Task<ShopResponseDTO?> GetByIdAsync(
        long shopId);

    Task<ShopResponseDTO?> GetMyShopByIdAsync(
        long ownerUserId,
        long shopId);

    Task<ShopResponseDTO?> UpdateAsync(
        long ownerUserId,
        long shopId,
        UpdateShopDTO request);
}