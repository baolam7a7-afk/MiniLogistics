using MiniLogistics.BLL.DTOs.ShopWallet;

namespace MiniLogistics.BLL.Services.ShopWallet;

public interface IShopWalletService
{
    Task<ShopWalletResponseDTO> CreateAsync(
        long ownerUserId,
        long shopId);

    Task<ShopWalletResponseDTO?> GetMyWalletAsync(
        long ownerUserId,
        long shopId);

    Task<ShopWalletResponseDTO?> GetByIdAsync(
        long walletId);
}