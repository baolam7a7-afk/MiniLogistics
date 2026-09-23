using MiniLogistics.BLL.DTOs.ShopWallet;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using ShopWalletModel = MiniLogistics.DAL.Models.ShopWallet;
using ShopModel = MiniLogistics.DAL.Models.Shop;
namespace MiniLogistics.BLL.Services.ShopWallet;

public class ShopWalletService : IShopWalletService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShopWalletService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // CREATE WALLET
    // =====================================================

    public async Task<ShopWalletResponseDTO> CreateAsync(
        long ownerUserId,
        long shopId)
    {
        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }

        // -------------------------------------------------
        // 1. Kiểm tra Shop
        // -------------------------------------------------

        var shop = await _unitOfWork.Shops
            .GetByIdAsync(shopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        // -------------------------------------------------
        // 2. Kiểm tra quyền sở hữu
        // -------------------------------------------------

        if (shop.OwnerUserId != ownerUserId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền tạo Wallet cho Shop này.");
        }

        // -------------------------------------------------
        // 3. Kiểm tra Wallet đã tồn tại chưa
        // -------------------------------------------------

        var existingWallet =
            await _unitOfWork.ShopWallets
                .FindAsync(x => x.ShopId == shopId);

        if (existingWallet.Any())
        {
            throw new BadRequestException(
                "Shop này đã có Wallet.");
        }

        // -------------------------------------------------
        // 4. Tạo Wallet
        // -------------------------------------------------

        var wallet = new ShopWalletModel
        {
            ShopId = shopId,

            // Wallet mới luôn bắt đầu từ 0
            Balance = 0m,

            UpdatedAt = null
        };

        await _unitOfWork.ShopWallets
            .AddAsync(wallet);

        await _unitOfWork.SaveChangesAsync();

        // -------------------------------------------------
        // 5. Response
        // -------------------------------------------------

        return MapToResponse(wallet, shop);
    }


    // =====================================================
    // GET MY WALLET
    // =====================================================

    public async Task<ShopWalletResponseDTO?> GetMyWalletAsync(
        long ownerUserId,
        long shopId)
    {
        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }

        // -------------------------------------------------
        // 1. Tìm Shop
        // -------------------------------------------------

        var shop = await _unitOfWork.Shops
            .GetByIdAsync(shopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        // -------------------------------------------------
        // 2. Kiểm tra Seller có phải chủ Shop không
        // -------------------------------------------------

        if (shop.OwnerUserId != ownerUserId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem Wallet của Shop này.");
        }

        // -------------------------------------------------
        // 3. Tìm Wallet
        // -------------------------------------------------

        var wallets =
            await _unitOfWork.ShopWallets
                .FindAsync(x => x.ShopId == shopId);

        var wallet = wallets.FirstOrDefault();

        if (wallet == null)
        {
            return null;
        }

        return MapToResponse(wallet, shop);
    }


    // =====================================================
    // GET WALLET BY ID - ADMIN
    // =====================================================

    public async Task<ShopWalletResponseDTO?> GetByIdAsync(
        long walletId)
    {
        if (walletId <= 0)
        {
            throw new BadRequestException(
                "WalletId không hợp lệ.");
        }

        // -------------------------------------------------
        // 1. Tìm Wallet
        // -------------------------------------------------

        var wallets =
            await _unitOfWork.ShopWallets
                .FindAsync(x => x.Id == walletId);

        var wallet = wallets.FirstOrDefault();

        if (wallet == null)
        {
            return null;
        }

        // -------------------------------------------------
        // 2. Tìm Shop
        // -------------------------------------------------

        var shop = await _unitOfWork.Shops
            .GetByIdAsync(wallet.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {wallet.ShopId} của Wallet không tồn tại.");
        }

        return MapToResponse(wallet, shop);
    }


    // =====================================================
    // MAPPING
    // =====================================================

    private static ShopWalletResponseDTO MapToResponse(
        ShopWalletModel wallet,
        ShopModel  shop)
    {
        return new ShopWalletResponseDTO
        {
            Id = wallet.Id,

            ShopId = wallet.ShopId,

            ShopName = shop.Name,

            OwnerUserId = shop.OwnerUserId,

            Balance = wallet.Balance,

            UpdatedAt = wallet.UpdatedAt
        };
    }
}