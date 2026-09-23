using MiniLogistics.BLL.DTOs.ShopWalletTransaction;

namespace MiniLogistics.BLL.Services.ShopWalletTransaction;

public interface IShopWalletTransactionService
{
    Task<ShopWalletTransactionResponseDTO> CreateAsync(
        long adminUserId,
        CreateShopWalletTransactionDTO request);

    Task<IEnumerable<ShopWalletTransactionResponseDTO>>
        GetMyTransactionsAsync(
            long sellerId,
            long shopId);

    Task<IEnumerable<ShopWalletTransactionResponseDTO>>
        GetAllAsync();

    Task<ShopWalletTransactionResponseDTO?>
        GetByIdAsync(long transactionId);
}