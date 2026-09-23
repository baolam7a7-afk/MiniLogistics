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

        var method =
            request.Method
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
    // =====================================================

    public async Task<
        IEnumerable<RefundTransactionResponseDTO>>
        GetMyRefundsAsync(
            long customerId)
    {
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
            return Enumerable.Empty<
                RefundTransactionResponseDTO>();
        }


        var returnRequestIds =
            returnRequests
                .Select(x => x.Id)
                .ToHashSet();


        var refunds =
            await _unitOfWork.RefundTransactions
                .FindAsync(
                    x =>
                        returnRequestIds
                            .Contains(
                                x.ReturnRequestId));


        var result =
            new List<RefundTransactionResponseDTO>();


        foreach (
            var refund
            in refunds.OrderByDescending(
                x => x.CreatedAt))
        {
            var order =
                await _unitOfWork.Orders
                    .GetByIdAsync(
                        returnRequests
                            .First(x =>
                                x.Id
                                ==
                                refund.ReturnRequestId)
                            .OrderId);


            if (order == null)
                continue;


            result.Add(
                await BuildResponseAsync(
                    refund,
                    order));
        }


        return result;
    }


    // =====================================================
    // GET ALL - ADMIN
    // =====================================================

    public async Task<
        IEnumerable<RefundTransactionResponseDTO>>
        GetAllAsync()
    {
        var refunds =
            await _unitOfWork.RefundTransactions
                .GetAllAsync();


        var result =
            new List<RefundTransactionResponseDTO>();


        foreach (
            var refund
            in refunds.OrderByDescending(
                x => x.CreatedAt))
        {
            var returnRequest =
                await _unitOfWork.ReturnRequests
                    .GetByIdAsync(
                        refund.ReturnRequestId);

            if (returnRequest == null)
                continue;


            var order =
                await _unitOfWork.Orders
                    .GetByIdAsync(
                        returnRequest.OrderId);

            if (order == null)
                continue;


            result.Add(
                await BuildResponseAsync(
                    refund,
                    order));
        }


        return result;
    }


    // =====================================================
    // GET BY ID
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


        refund.Status =
            "done";

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