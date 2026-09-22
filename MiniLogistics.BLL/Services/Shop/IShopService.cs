using MiniLogistics.BLL.DTOs.Shop;

namespace MiniLogistics.BLL.Services.Shop;

public interface IShopService
{
    Task<ShopResponseDto> CreateShopAsync(
        long userId,
        CreateShopDto request
    );

    Task<ShopResponseDto> GetMyShopAsync(
        long userId
    );
}