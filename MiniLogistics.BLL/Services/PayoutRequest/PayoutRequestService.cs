using MiniLogistics.BLL.DTOs.PayoutRequest;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using PayoutRequestModel = MiniLogistics.DAL.Models.PayoutRequest;
using ShopWalletModel = MiniLogistics.DAL.Models.ShopWallet;
using ShopWalletTransactionModel = MiniLogistics.DAL.Models.ShopWalletTransaction;

namespace MiniLogistics.BLL.Services.PayoutRequest;

public class PayoutRequestService : IPayoutRequestService
{
    private readonly IUnitOfWork _unitOfWork;

    public PayoutRequestService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // CREATE PAYOUT REQUEST - SELLER
    // =====================================================

    public async Task<PayoutRequestResponseDTO> CreateAsync(
        long userId,
        CreatePayoutRequestDTO request)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");
        }

        if (request == null)
        {
            throw new BadRequestException(
                "Dữ liệu yêu cầu không được để trống.");
        }

        if (request.ShopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }

        if (request.Amount <= 0)
        {
            throw new BadRequestException(
                "Số tiền rút phải lớn hơn 0.");
        }

        if (string.IsNullOrWhiteSpace(request.BankAccountName))
        {
            throw new BadRequestException(
                "Tên chủ tài khoản không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.BankAccountNumber))
        {
            throw new BadRequestException(
                "Số tài khoản không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.BankName))
        {
            throw new BadRequestException(
                "Tên ngân hàng không được để trống.");
        }

        // -------------------------------------------------
        // 1. Tìm Shop
        // -------------------------------------------------

        var shop = await _unitOfWork.Shops
            .GetByIdAsync(request.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {request.ShopId} không tồn tại.");
        }

        // -------------------------------------------------
        // 2. Kiểm tra quyền sở hữu Shop
        // -------------------------------------------------

        if (shop.OwnerUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền tạo yêu cầu rút tiền cho Shop này.");
        }

        // -------------------------------------------------
        // 3. Shop phải được Admin duyệt
        // -------------------------------------------------

        if (!string.Equals(
                shop.Status?.Trim(),
                "approved",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                $"Shop chưa được Admin duyệt. Trạng thái hiện tại: '{shop.Status}'.");
        }

        // -------------------------------------------------
        // 4. Tìm Wallet của Shop
        // -------------------------------------------------

        var wallets = await _unitOfWork.ShopWallets
            .FindAsync(x => x.ShopId == request.ShopId);

        var wallet = wallets.FirstOrDefault();

        if (wallet == null)
        {
            throw new NotFoundException(
                $"Shop {request.ShopId} chưa có Wallet.");
        }

        // -------------------------------------------------
        // 5. Kiểm tra số dư
        // -------------------------------------------------

        if (request.Amount > wallet.Balance)
        {
            throw new BadRequestException(
                $"Số dư Wallet không đủ. Số dư hiện tại: {wallet.Balance:N0}.");
        }

        // -------------------------------------------------
        // 6. Kiểm tra yêu cầu đang xử lý
        // -------------------------------------------------

        var existingRequests = await _unitOfWork.PayoutRequests
            .FindAsync(x =>
                x.ShopId == request.ShopId &&
                x.Status == "requested");

        if (existingRequests.Any())
        {
            throw new BadRequestException(
                "Shop đang có một yêu cầu rút tiền chưa được xử lý.");
        }

        // -------------------------------------------------
        // 7. Tạo PayoutRequest
        // -------------------------------------------------

        var payoutRequest = new PayoutRequestModel
        {
            ShopId = request.ShopId,

            Amount = request.Amount,

            BankAccountName =
                request.BankAccountName.Trim(),

            BankAccountNumber =
                request.BankAccountNumber.Trim(),

            BankName =
                request.BankName.Trim(),

            Status = "requested",

            RequestedAt = DateTime.UtcNow,

            ProcessedByUserId = null,

            ProcessedAt = null
        };

        await _unitOfWork.PayoutRequests
            .AddAsync(payoutRequest);

        await _unitOfWork.SaveChangesAsync();

        // -------------------------------------------------
        // 8. Response
        // -------------------------------------------------

        return MapToResponse(
            payoutRequest,
            shop.Name);
    }


    // =====================================================
    // GET MY PAYOUT REQUESTS - SELLER
    // =====================================================

    public async Task<IEnumerable<PayoutRequestResponseDTO>> GetMyAsync(
        long userId,
        long shopId)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");
        }

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
        // 2. Kiểm tra ownership
        // -------------------------------------------------

        if (shop.OwnerUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem yêu cầu rút tiền của Shop này.");
        }

        // -------------------------------------------------
        // 3. Lấy các PayoutRequest
        // -------------------------------------------------

        var requests = await _unitOfWork.PayoutRequests
            .FindAsync(x => x.ShopId == shopId);

        return requests
            .OrderByDescending(x => x.RequestedAt)
            .Select(x =>
                MapToResponse(
                    x,
                    shop.Name))
            .ToList();
    }


    // =====================================================
    // GET ALL - ADMIN
    // =====================================================

    public async Task<IEnumerable<PayoutRequestResponseDTO>> GetAllAsync()
    {
        var requests = await _unitOfWork.PayoutRequests
            .GetAllAsync();

        var result = new List<PayoutRequestResponseDTO>();

        foreach (var request in requests
                     .OrderByDescending(x => x.RequestedAt))
        {
            var shop = await _unitOfWork.Shops
                .GetByIdAsync(request.ShopId);

            result.Add(
                MapToResponse(
                    request,
                    shop?.Name));
        }

        return result;
    }


    // =====================================================
    // GET BY ID - ADMIN
    // =====================================================

    public async Task<PayoutRequestResponseDTO?> GetByIdAsync(
        long payoutRequestId)
    {
        if (payoutRequestId <= 0)
        {
            throw new BadRequestException(
                "PayoutRequestId không hợp lệ.");
        }

        var payoutRequest = await _unitOfWork.PayoutRequests
            .GetByIdAsync(payoutRequestId);

        if (payoutRequest == null)
        {
            return null;
        }

        var shop = await _unitOfWork.Shops
            .GetByIdAsync(payoutRequest.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {payoutRequest.ShopId} không tồn tại.");
        }

        return MapToResponse(
            payoutRequest,
            shop.Name);
    }


    // =====================================================
    // APPROVE - ADMIN
    // =====================================================

    public async Task<PayoutRequestResponseDTO> ApproveAsync(
        long adminUserId,
        long payoutRequestId)
    {
        if (adminUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Admin User ID không hợp lệ.");
        }

        if (payoutRequestId <= 0)
        {
            throw new BadRequestException(
                "PayoutRequestId không hợp lệ.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                // -----------------------------------------
                // 1. Tìm PayoutRequest
                // -----------------------------------------

                var payoutRequest =
                    await _unitOfWork.PayoutRequests
                        .GetByIdAsync(payoutRequestId);

                if (payoutRequest == null)
                {
                    throw new NotFoundException(
                        $"PayoutRequest {payoutRequestId} không tồn tại.");
                }

                // -----------------------------------------
                // 2. Chỉ request = requested mới được approve
                // -----------------------------------------

                if (!string.Equals(
                        payoutRequest.Status,
                        "requested",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestException(
                        $"Không thể approve PayoutRequest đang ở trạng thái '{payoutRequest.Status}'.");
                }

                // -----------------------------------------
                // 3. Tìm Shop
                // -----------------------------------------

                var shop = await _unitOfWork.Shops
                    .GetByIdAsync(payoutRequest.ShopId);

                if (shop == null)
                {
                    throw new NotFoundException(
                        $"Shop {payoutRequest.ShopId} không tồn tại.");
                }

                // -----------------------------------------
                // 4. Tìm Wallet
                // -----------------------------------------

                var wallets = await _unitOfWork.ShopWallets
                    .FindAsync(x =>
                        x.ShopId == payoutRequest.ShopId);

                var wallet = wallets.FirstOrDefault();

                if (wallet == null)
                {
                    throw new NotFoundException(
                        $"Shop {payoutRequest.ShopId} chưa có Wallet.");
                }

                // -----------------------------------------
                // 5. Kiểm tra Balance
                // -----------------------------------------

                if (payoutRequest.Amount > wallet.Balance)
                {
                    throw new BadRequestException(
                        $"Số dư Wallet không đủ. Số dư hiện tại: {wallet.Balance:N0}.");
                }

                // -----------------------------------------
                // 6. Trừ tiền Wallet
                // -----------------------------------------

                wallet.Balance -= payoutRequest.Amount;

                wallet.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ShopWallets
                    .Update(wallet);

                // -----------------------------------------
                // 7. Tạo Wallet Transaction
                // -----------------------------------------

                var transaction =
                    new ShopWalletTransactionModel
                    {
                        WalletId = wallet.Id,

                        OrderId = null,

                        Type = "PAYOUT_DEBIT",

                        Amount = payoutRequest.Amount,

                        Description =
                            $"Payout request #{payoutRequest.Id}",

                        CreatedAt = DateTime.UtcNow
                    };

                await _unitOfWork.ShopWalletTransactions
                    .AddAsync(transaction);

                // -----------------------------------------
                // 8. Update PayoutRequest
                // -----------------------------------------

                payoutRequest.Status = "approved";

                payoutRequest.ProcessedByUserId =
                    adminUserId;

                payoutRequest.ProcessedAt =
                    DateTime.UtcNow;

                _unitOfWork.PayoutRequests
                    .Update(payoutRequest);

                // -----------------------------------------
                // 9. Save
                // -----------------------------------------

                // ExecuteInTransactionAsync()
                // sẽ SaveChanges + Commit transaction.

                return MapToResponse(
                    payoutRequest,
                    shop.Name);
            });
    }


    // =====================================================
    // REJECT - ADMIN
    // =====================================================

    public async Task<PayoutRequestResponseDTO> RejectAsync(
        long adminUserId,
        long payoutRequestId)
    {
        if (adminUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Admin User ID không hợp lệ.");
        }

        if (payoutRequestId <= 0)
        {
            throw new BadRequestException(
                "PayoutRequestId không hợp lệ.");
        }

        var payoutRequest =
            await _unitOfWork.PayoutRequests
                .GetByIdAsync(payoutRequestId);

        if (payoutRequest == null)
        {
            throw new NotFoundException(
                $"PayoutRequest {payoutRequestId} không tồn tại.");
        }

        // -------------------------------------------------
        // Chỉ request = requested mới được reject
        // -------------------------------------------------

        if (!string.Equals(
                payoutRequest.Status,
                "requested",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                $"Không thể reject PayoutRequest đang ở trạng thái '{payoutRequest.Status}'.");
        }

        var shop = await _unitOfWork.Shops
            .GetByIdAsync(payoutRequest.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {payoutRequest.ShopId} không tồn tại.");
        }

        // -------------------------------------------------
        // Update status
        // -------------------------------------------------

        payoutRequest.Status = "rejected";

        payoutRequest.ProcessedByUserId =
            adminUserId;

        payoutRequest.ProcessedAt =
            DateTime.UtcNow;

        _unitOfWork.PayoutRequests
            .Update(payoutRequest);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(
            payoutRequest,
            shop.Name);
    }


    // =====================================================
    // MAPPING
    // =====================================================

    private static PayoutRequestResponseDTO MapToResponse(
        PayoutRequestModel payoutRequest,
        string? shopName)
    {
        return new PayoutRequestResponseDTO
        {
            Id = payoutRequest.Id,

            ShopId = payoutRequest.ShopId,

            ShopName = shopName,

            Amount = payoutRequest.Amount,

            BankAccountName =
                payoutRequest.BankAccountName,

            BankAccountNumber =
                payoutRequest.BankAccountNumber,

            BankName =
                payoutRequest.BankName,

            Status =
                payoutRequest.Status,

            RequestedAt =
                payoutRequest.RequestedAt,

            ProcessedByUserId =
                payoutRequest.ProcessedByUserId,

            ProcessedAt =
                payoutRequest.ProcessedAt
        };
    }
}