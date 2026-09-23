using MiniLogistics.BLL.DTOs.ShopWalletTransaction;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using ShopWalletTransactionModel =
    MiniLogistics.DAL.Models.ShopWalletTransaction;

using ShopWalletModel =
    MiniLogistics.DAL.Models.ShopWallet;

namespace MiniLogistics.BLL.Services.ShopWalletTransaction;

public class ShopWalletTransactionService
    : IShopWalletTransactionService
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly HashSet<string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "SALE_CREDIT",
            "REFUND_DEBIT",
            "PAYOUT_DEBIT",
            "ADJUSTMENT"
        };

    public ShopWalletTransactionService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // CREATE TRANSACTION - ADMIN
    // =====================================================

    public async Task<ShopWalletTransactionResponseDTO> CreateAsync(
        long adminUserId,
        CreateShopWalletTransactionDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // -------------------------------------------------
        // 1. Validate WalletId
        // -------------------------------------------------

        if (request.WalletId <= 0)
        {
            throw new BadRequestException(
                "WalletId không hợp lệ.");
        }

        // -------------------------------------------------
        // 2. Validate Type
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new BadRequestException(
                "Type không được để trống.");
        }

        var type = request.Type
            .Trim()
            .ToUpperInvariant();

        if (!AllowedTypes.Contains(type))
        {
            throw new BadRequestException(
                "Type phải là SALE_CREDIT, REFUND_DEBIT, PAYOUT_DEBIT hoặc ADJUSTMENT.");
        }

        // -------------------------------------------------
        // 3. Validate Amount
        // -------------------------------------------------

        if (request.Amount <= 0)
        {
            throw new BadRequestException(
                "Amount phải lớn hơn 0.");
        }

        if (request.Amount > 9999999999999999.99m)
        {
            throw new BadRequestException(
                "Amount vượt quá giới hạn cho phép.");
        }

        // -------------------------------------------------
        // 4. Check Admin
        // -------------------------------------------------

        var admin = await _unitOfWork.Users
            .GetByIdAsync(adminUserId);

        if (admin == null)
        {
            throw new NotFoundException(
                $"User {adminUserId} không tồn tại.");
        }

        if (admin.Status != "active")
        {
            throw new BadRequestException(
                "Tài khoản Admin không hoạt động.");
        }

        // -------------------------------------------------
        // 5. Get Wallet
        // -------------------------------------------------

        var wallet = await _unitOfWork.ShopWallets
            .GetByIdAsync(request.WalletId);

        if (wallet == null)
        {
            throw new NotFoundException(
                $"Wallet {request.WalletId} không tồn tại.");
        }

        // -------------------------------------------------
        // 6. Get Shop
        // -------------------------------------------------

        var shop = await _unitOfWork.Shops
            .GetByIdAsync(wallet.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {wallet.ShopId} không tồn tại.");
        }

        // -------------------------------------------------
        // 7. Validate Order nếu có
        // -------------------------------------------------

        if (request.OrderId.HasValue)
        {
            if (request.OrderId.Value <= 0)
            {
                throw new BadRequestException(
                    "OrderId không hợp lệ.");
            }

            var order = await _unitOfWork.Orders
                .GetByIdAsync(request.OrderId.Value);

            if (order == null)
            {
                throw new NotFoundException(
                    $"Order {request.OrderId.Value} không tồn tại.");
            }

            if (order.ShopId != wallet.ShopId)
            {
                throw new BadRequestException(
                    "Order không thuộc Shop của Wallet.");
            }
        }

        // -------------------------------------------------
        // 8. Description
        // -------------------------------------------------

        string? description = null;

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            description = request.Description.Trim();

            if (description.Length > 1000)
            {
                throw new BadRequestException(
                    "Description tối đa 1000 ký tự.");
            }
        }

        // -------------------------------------------------
        // 9. Calculate Balance
        // -------------------------------------------------

        var oldBalance = wallet.Balance;

        decimal newBalance;

        if (IsCredit(type))
        {
            // SALE_CREDIT
            newBalance = oldBalance + request.Amount;
        }
        else
        {
            // REFUND_DEBIT
            // PAYOUT_DEBIT
            // ADJUSTMENT

            if (oldBalance < request.Amount)
            {
                throw new BadRequestException(
                    $"Số dư Wallet không đủ. Balance hiện tại: {oldBalance:N2}.");
            }

            newBalance = oldBalance - request.Amount;
        }

        // -------------------------------------------------
        // 10. Create Transaction
        // -------------------------------------------------

        var transaction =
            new ShopWalletTransactionModel
            {
                WalletId = wallet.Id,

                OrderId = request.OrderId,

                Type = type,

                Amount = request.Amount,

                Description = description,

                CreatedAt = DateTime.UtcNow
            };

        await _unitOfWork.ShopWalletTransactions
            .AddAsync(transaction);

        // -------------------------------------------------
        // 11. Update Wallet Balance
        // -------------------------------------------------

        wallet.Balance = newBalance;

        wallet.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ShopWallets.Update(wallet);

        // -------------------------------------------------
        // 12. Save
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();

        // -------------------------------------------------
        // 13. Return
        // -------------------------------------------------

        return MapToResponse(
            transaction,
            wallet,
            shop,
            newBalance);
    }


    // =====================================================
    // GET MY TRANSACTIONS - SELLER
    // =====================================================

    public async Task<IEnumerable<ShopWalletTransactionResponseDTO>>
        GetMyTransactionsAsync(
            long sellerId,
            long shopId)
    {
        // -------------------------------------------------
        // 1. Validate ShopId
        // -------------------------------------------------

        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }

        // -------------------------------------------------
        // 2. Get Shop
        // -------------------------------------------------

        var shop = await _unitOfWork.Shops
            .GetByIdAsync(shopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        // -------------------------------------------------
        // 3. Check Ownership
        // -------------------------------------------------

        if (shop.OwnerUserId != sellerId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem giao dịch của Shop này.");
        }

        // -------------------------------------------------
        // 4. Get Wallet
        // -------------------------------------------------

        var wallets = await _unitOfWork.ShopWallets
            .FindAsync(x => x.ShopId == shopId);

        var wallet = wallets.FirstOrDefault();

        if (wallet == null)
        {
            throw new NotFoundException(
                "Shop này chưa có Wallet.");
        }

        // -------------------------------------------------
        // 5. Get Transactions
        // -------------------------------------------------

        var transactions =
            await _unitOfWork.ShopWalletTransactions
                .FindAsync(
                    x => x.WalletId == wallet.Id);

        // -------------------------------------------------
        // 6. Return
        // -------------------------------------------------

        return transactions
            .OrderByDescending(x => x.CreatedAt)
            .Select(x =>
                MapToResponse(
                    x,
                    wallet,
                    shop,
                    null))
            .ToList();
    }


    // =====================================================
    // GET ALL - ADMIN
    // =====================================================

    public async Task<IEnumerable<ShopWalletTransactionResponseDTO>>
        GetAllAsync()
    {
        var transactions =
            await _unitOfWork.ShopWalletTransactions
                .GetAllAsync();

        var result =
            new List<ShopWalletTransactionResponseDTO>();

        foreach (var transaction in
                 transactions.OrderByDescending(
                     x => x.CreatedAt))
        {
            // ---------------------------------------------
            // Get Wallet
            // ---------------------------------------------

            var wallet =
                await _unitOfWork.ShopWallets
                    .GetByIdAsync(transaction.WalletId);

            if (wallet == null)
            {
                continue;
            }

            // ---------------------------------------------
            // Get Shop
            // ---------------------------------------------

            var shop =
                await _unitOfWork.Shops
                    .GetByIdAsync(wallet.ShopId);

            if (shop == null)
            {
                continue;
            }

            // ---------------------------------------------
            // Add result
            // ---------------------------------------------

            result.Add(
                MapToResponse(
                    transaction,
                    wallet,
                    shop,
                    null));
        }

        return result;
    }


    // =====================================================
    // GET BY ID - ADMIN
    // =====================================================

    public async Task<ShopWalletTransactionResponseDTO?>
        GetByIdAsync(long transactionId)
    {
        // -------------------------------------------------
        // 1. Validate TransactionId
        // -------------------------------------------------

        if (transactionId <= 0)
        {
            throw new BadRequestException(
                "TransactionId không hợp lệ.");
        }

        // -------------------------------------------------
        // 2. Get Transaction
        // -------------------------------------------------

        var transaction =
            await _unitOfWork.ShopWalletTransactions
                .GetByIdAsync(transactionId);

        if (transaction == null)
        {
            return null;
        }

        // -------------------------------------------------
        // 3. Get Wallet
        // -------------------------------------------------

        var wallet =
            await _unitOfWork.ShopWallets
                .GetByIdAsync(transaction.WalletId);

        if (wallet == null)
        {
            throw new NotFoundException(
                $"Wallet {transaction.WalletId} không tồn tại.");
        }

        // -------------------------------------------------
        // 4. Get Shop
        // -------------------------------------------------

        var shop =
            await _unitOfWork.Shops
                .GetByIdAsync(wallet.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {wallet.ShopId} không tồn tại.");
        }

        // -------------------------------------------------
        // 5. Return
        // -------------------------------------------------

        return MapToResponse(
            transaction,
            wallet,
            shop,
            null);
    }


    // =====================================================
    // CHECK CREDIT
    // =====================================================

    private static bool IsCredit(string type)
    {
        return type == "SALE_CREDIT";
    }


    // =====================================================
    // MAP RESPONSE
    // =====================================================

    private static ShopWalletTransactionResponseDTO
        MapToResponse(
            ShopWalletTransactionModel transaction,
            ShopWalletModel wallet,
            MiniLogistics.DAL.Models.Shop shop,
            decimal? balanceAfter)
    {
        return new ShopWalletTransactionResponseDTO
        {
            Id = transaction.Id,

            WalletId = transaction.WalletId,

            ShopId = wallet.ShopId,

            ShopName = shop.Name,

            OrderId = transaction.OrderId,

            Type = transaction.Type,

            Amount = transaction.Amount,

            BalanceAfter = balanceAfter,

            Description = transaction.Description,

            CreatedAt = transaction.CreatedAt
        };
    }
}