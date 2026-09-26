using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.RefundTransaction;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using RefundTransactionModel =
    MiniLogistics.DAL.Models.RefundTransaction;

namespace MiniLogistics.BLL.Services.RefundTransaction;

public class RefundTransactionService
    : IRefundTransactionService
{
    private readonly IUnitOfWork _unitOfWork;

    public RefundTransactionService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // CREATE REFUND
    // =====================================================

    public async Task<RefundTransactionResponseDTO>
        CreateAsync(
            long adminUserId,
            CreateRefundTransactionDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        // -------------------------------------------------
        // VALIDATE RETURN REQUEST ID
        // -------------------------------------------------

        if (request.ReturnRequestId <= 0)
        {
            throw new BadRequestException(
                "ReturnRequestId không hợp lệ.");
        }

        // -------------------------------------------------
        // VALIDATE AMOUNT
        // -------------------------------------------------

        if (request.Amount <= 0)
        {
            throw new BadRequestException(
                "Amount phải lớn hơn 0.");
        }

        // -------------------------------------------------
        // VALIDATE METHOD
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Method))
        {
            throw new BadRequestException(
                "Method không được để trống.");
        }

        var method = request.Method
            .Trim()
            .ToLowerInvariant();

        var allowedMethods = new[]
        {
            "original",
            "wallet",
            "bank"
        };

        if (!allowedMethods.Contains(method))
        {
            throw new BadRequestException(
                "Method phải là original, wallet hoặc bank.");
        }

        // -------------------------------------------------
        // CHECK ADMIN
        // -------------------------------------------------

        var admin =
            await _unitOfWork.Users
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
        // GET RETURN REQUEST
        // -------------------------------------------------

        var returnRequest =
            await _unitOfWork.ReturnRequests
                .GetByIdAsync(
                    request.ReturnRequestId);

        if (returnRequest == null)
        {
            throw new NotFoundException(
                $"ReturnRequest {request.ReturnRequestId} không tồn tại.");
        }

        // -------------------------------------------------
        // RETURN REQUEST MUST BE APPROVED
        // -------------------------------------------------

        var returnStatus =
            returnRequest.Status
                .Trim()
                .ToLowerInvariant();

        if (returnStatus != "approved")
        {
            throw new BadRequestException(
                $"Chỉ có thể tạo RefundTransaction khi ReturnRequest ở trạng thái 'approved'. " +
                $"Trạng thái hiện tại: '{returnRequest.Status}'.");
        }

        // -------------------------------------------------
        // GET ORDER
        // -------------------------------------------------

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(
                    returnRequest.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                $"Order {returnRequest.OrderId} không tồn tại.");
        }

        // -------------------------------------------------
        // VALIDATE AMOUNT
        // -------------------------------------------------

        if (request.Amount > order.Total)
        {
            throw new BadRequestException(
                $"Amount không được lớn hơn Total của Order ({order.Total}).");
        }

        // -------------------------------------------------
        // CHECK EXISTING REFUND
        // -------------------------------------------------

        var existingRefunds =
            await _unitOfWork.RefundTransactions
                .FindAsync(
                    x =>
                        x.ReturnRequestId
                            == returnRequest.Id
                        &&
                        (
                            x.Status == "pending"
                            ||
                            x.Status == "done"
                        ));

        if (existingRefunds.Any())
        {
            throw new BadRequestException(
                "ReturnRequest này đã có giao dịch hoàn tiền.");
        }

        // -------------------------------------------------
        // CREATE
        // -------------------------------------------------

        var refund =
            new RefundTransactionModel
            {
                ReturnRequestId =
                    returnRequest.Id,

                Amount =
                    request.Amount,

                Method =
                    method,

                Status =
                    "pending",

                CreatedAt =
                    DateTime.UtcNow,

                CompletedAt =
                    null
            };

        await _unitOfWork.RefundTransactions
            .AddAsync(refund);

        await _unitOfWork
            .SaveChangesAsync();

        return await BuildResponseAsync(
            refund,
            order);
    }

    // =====================================================
    // GET MY REFUNDS
    // CUSTOMER
    // =====================================================

    public async Task<
        PagedResponseDTO<RefundTransactionResponseDTO>>
        GetMyRefundsAsync(
            long customerId,
            RefundPaginationRequestDTO request)
    {
        ValidatePagination(request);

        var customer =
            await _unitOfWork.Users
                .GetByIdAsync(customerId);

        if (customer == null)
        {
            throw new NotFoundException(
                $"User {customerId} không tồn tại.");
        }

        var returnRequests =
            await _unitOfWork.ReturnRequests
                .FindAsync(
                    x =>
                        x.CustomerId
                        == customerId);

        if (!returnRequests.Any())
        {
            return CreateEmptyResponse(request);
        }

        var returnRequestIds =
            returnRequests
                .Select(x => x.Id)
                .ToHashSet();

        var refunds =
            await _unitOfWork.RefundTransactions
                .FindAsync(
                    x =>
                        returnRequestIds.Contains(
                            x.ReturnRequestId));

        return await BuildPagedResponseAsync(
            refunds,
            returnRequests,
            request);
    }

    // =====================================================
    // GET ALL
    // ADMIN
    // =====================================================

    public async Task<
        PagedResponseDTO<RefundTransactionResponseDTO>>
        GetAllAsync(
            RefundPaginationRequestDTO request)
    {
        ValidatePagination(request);

        var refunds =
            await _unitOfWork.RefundTransactions
                .GetAllAsync();

        if (!refunds.Any())
        {
            return CreateEmptyResponse(request);
        }

        var returnRequests =
            await _unitOfWork.ReturnRequests
                .GetAllAsync();

        return await BuildPagedResponseAsync(
            refunds,
            returnRequests,
            request);
    }

    // =====================================================
    // GET BY ID
    // CUSTOMER / ADMIN
    // =====================================================

    public async Task<
        RefundTransactionResponseDTO?>
        GetByIdAsync(
            long userId,
            string role,
            long refundId)
    {
        if (refundId <= 0)
        {
            throw new BadRequestException(
                "RefundTransactionId không hợp lệ.");
        }

        var refund =
            await _unitOfWork.RefundTransactions
                .GetByIdAsync(refundId);

        if (refund == null)
        {
            throw new NotFoundException(
                $"RefundTransaction {refundId} không tồn tại.");
        }

        var returnRequest =
            await _unitOfWork.ReturnRequests
                .GetByIdAsync(
                    refund.ReturnRequestId);

        if (returnRequest == null)
        {
            throw new NotFoundException(
                "ReturnRequest của RefundTransaction không tồn tại.");
        }

        role =
            role.Trim()
                .ToLowerInvariant();

        // -------------------------------------------------
        // ADMIN
        // -------------------------------------------------

        if (role == "admin")
        {
            var adminOrder =
                await _unitOfWork.Orders
                    .GetByIdAsync(
                        returnRequest.OrderId);

            if (adminOrder == null)
            {
                throw new NotFoundException(
                    "Order của RefundTransaction không tồn tại.");
            }

            return await BuildResponseAsync(
                refund,
                adminOrder);
        }

        // -------------------------------------------------
        // CUSTOMER
        // -------------------------------------------------

        if (role == "customer")
        {
            if (returnRequest.CustomerId
                != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem RefundTransaction này.");
            }

            var customerOrder =
                await _unitOfWork.Orders
                    .GetByIdAsync(
                        returnRequest.OrderId);

            if (customerOrder == null)
            {
                throw new NotFoundException(
                    "Order của RefundTransaction không tồn tại.");
            }

            return await BuildResponseAsync(
                refund,
                customerOrder);
        }

        throw new ForbiddenException(
            "Bạn không có quyền xem RefundTransaction này.");
    }

    // =====================================================
    // COMPLETE
    // =====================================================

    public async Task<
        RefundTransactionResponseDTO>
        CompleteAsync(
            long adminUserId,
            long refundId)
    {
        var refund =
            await GetRefundOrThrowAsync(
                refundId);

        await ValidateAdminAsync(
            adminUserId);

        var status =
            refund.Status
                .Trim()
                .ToLowerInvariant();

        if (status != "pending")
        {
            throw new BadRequestException(
                $"Không thể complete RefundTransaction đang ở trạng thái '{refund.Status}'.");
        }

        var returnRequest =
            await _unitOfWork.ReturnRequests
                .GetByIdAsync(
                    refund.ReturnRequestId);

        if (returnRequest == null)
        {
            throw new NotFoundException(
                "ReturnRequest không tồn tại.");
        }

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(
                    returnRequest.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }

        var wallets =
            await _unitOfWork.ShopWallets
                .FindAsync(
                    x => x.ShopId == order.ShopId);

        var shopWallet =
            wallets.FirstOrDefault();

        if (shopWallet == null)
        {
            throw new NotFoundException(
                $"Shop {order.ShopId} chưa có ShopWallet.");
        }

        if (shopWallet.Balance < refund.Amount)
        {
            throw new BadRequestException(
                $"ShopWallet không đủ số dư để hoàn tiền. " +
                $"Balance hiện tại: {shopWallet.Balance}, " +
                $"Refund: {refund.Amount}.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                // ---------------------------------------------
                // TRỪ SHOP WALLET
                // ---------------------------------------------

                shopWallet.Balance -= refund.Amount;
                shopWallet.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ShopWallets
                    .Update(shopWallet);

                // ---------------------------------------------
                // WALLET TRANSACTION
                // ---------------------------------------------

                var walletTransaction =
                    new MiniLogistics.DAL.Models.ShopWalletTransaction
                    {
                        WalletId = shopWallet.Id,
                        OrderId = order.Id,
                        Type = "REFUND_DEBIT",
                        Amount = refund.Amount,
                        Description =
                            $"Refund transaction #{refund.Id}",
                        CreatedAt = DateTime.UtcNow
                    };

                await _unitOfWork.ShopWalletTransactions
                    .AddAsync(walletTransaction);

                // ---------------------------------------------
                // COMPLETE REFUND
                // ---------------------------------------------

                refund.Status = "done";
                refund.CompletedAt = DateTime.UtcNow;

                _unitOfWork.RefundTransactions
                    .Update(refund);

                return await BuildResponseAsync(
                    refund,
                    order);
            });
    }

    // =====================================================
    // FAIL
    // =====================================================

    public async Task<
        RefundTransactionResponseDTO>
        FailAsync(
            long adminUserId,
            long refundId)
    {
        var refund =
            await GetRefundOrThrowAsync(
                refundId);

        await ValidateAdminAsync(
            adminUserId);

        var status =
            refund.Status
                .Trim()
                .ToLowerInvariant();

        if (status != "pending")
        {
            throw new BadRequestException(
                $"Không thể fail RefundTransaction đang ở trạng thái '{refund.Status}'.");
        }

        refund.Status =
            "failed";

        refund.CompletedAt =
            DateTime.UtcNow;

        _unitOfWork.RefundTransactions
            .Update(refund);

        await _unitOfWork
            .SaveChangesAsync();

        var returnRequest =
            await _unitOfWork.ReturnRequests
                .GetByIdAsync(
                    refund.ReturnRequestId);

        if (returnRequest == null)
        {
            throw new NotFoundException(
                "ReturnRequest không tồn tại.");
        }

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(
                    returnRequest.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }

        return await BuildResponseAsync(
            refund,
            order);
    }

    // =====================================================
    // PAGINATION
    // =====================================================

    private async Task<
        PagedResponseDTO<RefundTransactionResponseDTO>>
        BuildPagedResponseAsync(
            IEnumerable<RefundTransactionModel> refunds,
            IEnumerable<MiniLogistics.DAL.Models.ReturnRequest> returnRequests,
            RefundPaginationRequestDTO request)
    {
        ValidatePagination(request);

        var returnRequestList =
            returnRequests.ToList();

        var result =
            new List<RefundTransactionResponseDTO>();

        foreach (
            var refund
            in refunds.OrderByDescending(
                x => x.CreatedAt))
        {
            var returnRequest =
                returnRequestList.FirstOrDefault(
                    x =>
                        x.Id
                        == refund.ReturnRequestId);

            if (returnRequest == null)
            {
                continue;
            }

            var order =
                await _unitOfWork.Orders
                    .GetByIdAsync(
                        returnRequest.OrderId);

            if (order == null)
            {
                continue;
            }

            result.Add(
                await BuildResponseAsync(
                    refund,
                    order));
        }

        // =================================================
        // STATUS FILTER
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Status))
        {
            var status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            ValidateStatus(status);

            result =
                result
                    .Where(
                        x =>
                            x.Status != null
                            &&
                            x.Status
                                .Trim()
                                .ToLowerInvariant()
                            == status)
                    .ToList();
        }

        // =================================================
        // RETURN REQUEST FILTER
        // =================================================

        if (request.ReturnRequestId.HasValue)
        {
            result =
                result
                    .Where(
                        x =>
                            x.ReturnRequestId
                            == request.ReturnRequestId.Value)
                    .ToList();
        }

        // =================================================
        // ORDER FILTER
        // =================================================

        if (request.OrderId.HasValue)
        {
            result =
                result
                    .Where(
                        x =>
                            x.OrderId
                            == request.OrderId.Value)
                    .ToList();
        }

        // =================================================
        // SEARCH
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Search))
        {
            var search =
                request.Search.Trim();

            result =
                result
                    .Where(
                        x =>
                            (
                                !string.IsNullOrWhiteSpace(
                                    x.OrderCode)
                                &&
                                x.OrderCode.Contains(
                                    search,
                                    StringComparison.OrdinalIgnoreCase)
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    x.CustomerEmail)
                                &&
                                x.CustomerEmail.Contains(
                                    search,
                                    StringComparison.OrdinalIgnoreCase)
                            ))
                    .ToList();
        }

        // =================================================
        // PAGINATION
        // =================================================

        var totalItems =
            result.Count;

        var totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)request.PageSize);

        var items =
            result
                .Skip(
                    (request.Page - 1)
                    * request.PageSize)
                .Take(
                    request.PageSize)
                .ToList();

        return new PagedResponseDTO<
            RefundTransactionResponseDTO>
        {
            Items = items,

            Page = request.Page,

            PageSize = request.PageSize,

            TotalItems = totalItems,

            TotalPages = totalPages
        };
    }

    // =====================================================
    // EMPTY RESPONSE
    // =====================================================

    private PagedResponseDTO<
        RefundTransactionResponseDTO>
        CreateEmptyResponse(
            RefundPaginationRequestDTO request)
    {
        return new PagedResponseDTO<
            RefundTransactionResponseDTO>
        {
            Items = new(),

            Page = request.Page,

            PageSize = request.PageSize,

            TotalItems = 0,

            TotalPages = 0
        };
    }

    // =====================================================
    // VALIDATE PAGINATION
    // =====================================================

    private void ValidatePagination(
        RefundPaginationRequestDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Pagination request không được null.");
        }

        if (request.Page < 1)
        {
            throw new BadRequestException(
                "Page phải >= 1.");
        }

        if (request.PageSize < 1 ||
            request.PageSize > 100)
        {
            throw new BadRequestException(
                "PageSize phải từ 1 đến 100.");
        }
    }

    // =====================================================
    // VALIDATE STATUS
    // =====================================================

    private void ValidateStatus(
        string status)
    {
        var allowedStatuses =
            new[]
            {
                "pending",
                "done",
                "failed"
            };

        if (!allowedStatuses.Contains(
                status))
        {
            throw new BadRequestException(
                "Status không hợp lệ. " +
                "Chỉ chấp nhận: pending, done, failed.");
        }
    }

    // =====================================================
    // GET REFUND
    // =====================================================

    private async Task<
        RefundTransactionModel>
        GetRefundOrThrowAsync(
            long refundId)
    {
        if (refundId <= 0)
        {
            throw new BadRequestException(
                "RefundTransactionId không hợp lệ.");
        }

        var refund =
            await _unitOfWork.RefundTransactions
                .GetByIdAsync(refundId);

        if (refund == null)
        {
            throw new NotFoundException(
                $"RefundTransaction {refundId} không tồn tại.");
        }

        return refund;
    }

    // =====================================================
    // VALIDATE ADMIN
    // =====================================================

    private async Task ValidateAdminAsync(
        long adminUserId)
    {
        var admin =
            await _unitOfWork.Users
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
    }

    // =====================================================
    // MAPPING
    // =====================================================

    private async Task<
        RefundTransactionResponseDTO>
        BuildResponseAsync(
            RefundTransactionModel refund,
            MiniLogistics.DAL.Models.Order order)
    {
        var customer =
            await _unitOfWork.Users
                .GetByIdAsync(
                    order.CustomerId);

        return new RefundTransactionResponseDTO
        {
            Id =
                refund.Id,

            ReturnRequestId =
                refund.ReturnRequestId,

            OrderId =
                order.Id,

            OrderCode =
                order.OrderCode,

            CustomerId =
                order.CustomerId,

            CustomerEmail =
                customer?.Email,

            Amount =
                refund.Amount,

            Method =
                refund.Method,

            Status =
                refund.Status,

            CreatedAt =
                refund.CreatedAt,

            CompletedAt =
                refund.CompletedAt
        };
    }
}