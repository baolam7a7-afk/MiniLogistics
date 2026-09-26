using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Payment;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

using OrderModel = MiniLogistics.DAL.Models.Order;

namespace MiniLogistics.BLL.Services.Payment;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // ADMIN
    // =====================================================

    public async Task<PagedResponseDTO<PaymentResponseDTO>>
        GetAllAsync(
            PaymentPaginationRequestDTO request)
    {
        ValidatePagination(request);

        var payments =
            await _unitOfWork.PaymentTransactions
                .GetAllAsync();

        var result =
            new List<PaymentResponseDTO>();

        foreach (var payment in payments)
        {
            var order =
                await _unitOfWork.Orders
                    .GetByIdAsync(payment.OrderId);

            if (order == null)
            {
                continue;
            }

            result.Add(
                MapToResponse(
                    payment,
                    order));
        }

        // =================================================
        // SEARCH
        // =================================================

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            result = result
                .Where(x =>
                    x.OrderCode
                        .ToLowerInvariant()
                        .Contains(search))
                .ToList();
        }

        // =================================================
        // STATUS
        // =================================================

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            result = result
                .Where(x =>
                    x.Status
                        .Equals(
                            status,
                            StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // =================================================
        // ORDER ID
        // =================================================

        if (request.OrderId.HasValue)
        {
            result = result
                .Where(x =>
                    x.OrderId ==
                    request.OrderId.Value)
                .ToList();
        }

        // =================================================
        // SORT
        // =================================================

        result = result
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        // =================================================
        // PAGINATION
        // =================================================

        var totalItems =
            result.Count;

        var totalPages =
            (int)Math.Ceiling(
                totalItems /
                (double)request.PageSize);

        var items =
            result
                .Skip(
                    (request.Page - 1) *
                    request.PageSize)
                .Take(
                    request.PageSize)
                .ToList();

        return new PagedResponseDTO<PaymentResponseDTO>
        {
            Items = items,

            Page =
                request.Page,

            PageSize =
                request.PageSize,

            TotalItems =
                totalItems,

            TotalPages =
                totalPages
        };
    }


    // =====================================================
    // GET MY PAYMENTS
    // CUSTOMER
    // =====================================================

    public async Task<PagedResponseDTO<PaymentResponseDTO>>
        GetMyPaymentsAsync(
            long customerId,
            PaymentPaginationRequestDTO request)
    {
        if (customerId <= 0)
        {
            throw new BadRequestException(
                "Customer ID không hợp lệ.");
        }

        ValidatePagination(request);

        var orders =
            await _unitOfWork.Orders
                .FindAsync(
                    x =>
                        x.CustomerId ==
                        customerId);

        var result =
            new List<PaymentResponseDTO>();

        foreach (var order in orders)
        {
            var payments =
                await _unitOfWork
                    .PaymentTransactions
                    .FindAsync(
                        x =>
                            x.OrderId ==
                            order.Id);

            foreach (var payment in payments)
            {
                result.Add(
                    MapToResponse(
                        payment,
                        order));
            }
        }

        // =================================================
        // SEARCH
        // =================================================

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            result = result
                .Where(x =>
                    x.OrderCode
                        .ToLowerInvariant()
                        .Contains(search))
                .ToList();
        }

        // =================================================
        // STATUS
        // =================================================

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            result = result
                .Where(x =>
                    x.Status
                        .Equals(
                            status,
                            StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // =================================================
        // ORDER ID
        // =================================================

        if (request.OrderId.HasValue)
        {
            result = result
                .Where(x =>
                    x.OrderId ==
                    request.OrderId.Value)
                .ToList();
        }

        // =================================================
        // SORT
        // =================================================

        result = result
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        // =================================================
        // PAGINATION
        // =================================================

        var totalItems =
            result.Count;

        var totalPages =
            (int)Math.Ceiling(
                totalItems /
                (double)request.PageSize);

        var items =
            result
                .Skip(
                    (request.Page - 1) *
                    request.PageSize)
                .Take(
                    request.PageSize)
                .ToList();

        return new PagedResponseDTO<PaymentResponseDTO>
        {
            Items = items,

            Page =
                request.Page,

            PageSize =
                request.PageSize,

            TotalItems =
                totalItems,

            TotalPages =
                totalPages
        };
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<PaymentResponseDTO>
        GetByIdAsync(
            long paymentId,
            long userId,
            string role)
    {
        var payment =
            await _unitOfWork
                .PaymentTransactions
                .GetByIdAsync(paymentId);

        if (payment == null)
        {
            throw new NotFoundException(
                "Payment không tồn tại.");
        }

        var order =
            await _unitOfWork
                .Orders
                .GetByIdAsync(
                    payment.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order của Payment không tồn tại.");
        }

        await CheckAccess(
            order,
            userId,
            role);

        return MapToResponse(
            payment,
            order);
    }


    // =====================================================
    // GET BY ORDER
    // =====================================================

    public async Task<PaymentResponseDTO>
        GetByOrderIdAsync(
            long orderId,
            long userId,
            string role)
    {
        var order =
            await _unitOfWork
                .Orders
                .GetByIdAsync(orderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }

        await CheckAccess(
            order,
            userId,
            role);

        var payments =
            await _unitOfWork
                .PaymentTransactions
                .FindAsync(
                    x =>
                        x.OrderId ==
                        orderId);

        var payment =
            payments
                .OrderByDescending(
                    x => x.CreatedAt)
                .FirstOrDefault();

        if (payment == null)
        {
            throw new NotFoundException(
                "Order này chưa có Payment.");
        }

        return MapToResponse(
            payment,
            order);
    }


    // =====================================================
    // UPDATE STATUS
    // ADMIN
    // =====================================================

    public async Task<PaymentResponseDTO>
        UpdateStatusAsync(
            long paymentId,
            UpdatePaymentStatusDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Status))
        {
            throw new BadRequestException(
                "Status không được để trống.");
        }

        var payment =
            await _unitOfWork
                .PaymentTransactions
                .GetByIdAsync(paymentId);

        if (payment == null)
        {
            throw new NotFoundException(
                "Payment không tồn tại.");
        }

        var order =
            await _unitOfWork
                .Orders
                .GetByIdAsync(
                    payment.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }

        string newStatus =
            request.Status
                .Trim()
                .ToLowerInvariant();

        ValidateStatus(
            payment.Status,
            newStatus);

        // =================================================
        // CURRENTLY ONLY COD
        // =================================================

        if (!string.Equals(
                order.PaymentMethod,
                "cod",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                "Project hiện tại chỉ hỗ trợ COD.");
        }

        // =================================================
        // UPDATE STATUS
        // =================================================

        payment.Status =
            newStatus;

        // =================================================
        // PROVIDER TRANSACTION ID
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.ProviderTxnId))
        {
            payment.ProviderTxnId =
                request.ProviderTxnId.Trim();
        }

        // =================================================
        // PAID TIME
        // =================================================

        if (newStatus == "paid")
        {
            payment.PaidAt =
                payment.PaidAt
                ?? DateTime.UtcNow;
        }
        else
        {
            payment.PaidAt = null;
        }

        _unitOfWork
            .PaymentTransactions
            .Update(payment);

        await _unitOfWork
            .SaveChangesAsync();

        return MapToResponse(
            payment,
            order);
    }


    // =====================================================
    // VALIDATE PAYMENT STATUS
    // =====================================================

    private void ValidateStatus(
        string currentStatus,
        string newStatus)
    {
        var allowed =
            new[]
            {
                "pending",
                "paid",
                "failed",
                "cancelled"
            };

        if (!allowed.Contains(
                newStatus))
        {
            throw new BadRequestException(
                "Payment status không hợp lệ.");
        }

        // =================================================
        // SAME STATUS
        // =================================================

        if (currentStatus == newStatus)
        {
            return;
        }

        // =================================================
        // TERMINAL STATUS
        // =================================================

        if (currentStatus == "paid")
        {
            throw new BadRequestException(
                "Payment đã paid và không thể thay đổi.");
        }

        if (currentStatus == "cancelled")
        {
            throw new BadRequestException(
                "Payment đã cancelled và không thể thay đổi.");
        }

        if (currentStatus == "failed")
        {
            throw new BadRequestException(
                "Payment đã failed và không thể thay đổi.");
        }

        // =================================================
        // PENDING
        // =================================================

        if (currentStatus == "pending" &&
            (newStatus == "paid" ||
             newStatus == "failed" ||
             newStatus == "cancelled"))
        {
            return;
        }

        throw new BadRequestException(
            $"Không thể chuyển Payment từ " +
            $"'{currentStatus}' sang '{newStatus}'.");
    }


    // =====================================================
    // ACCESS CONTROL
    // =====================================================

    private async Task CheckAccess(
        OrderModel order,
        long userId,
        string role)
    {
        role =
            role.Trim()
                .ToLowerInvariant();

        // =================================================
        // ADMIN
        // =================================================

        if (role == "admin")
        {
            return;
        }

        // =================================================
        // CUSTOMER
        // =================================================

        if (role == "customer")
        {
            if (order.CustomerId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Payment này.");
            }

            return;
        }

        // =================================================
        // SELLER
        // =================================================

        if (role == "seller")
        {
            bool ownsShop =
                await _unitOfWork
                    .Shops
                    .AnyAsync(
                        x =>
                            x.Id ==
                            order.ShopId &&
                            x.OwnerUserId ==
                            userId);

            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Payment này.");
            }

            return;
        }

        throw new ForbiddenException(
            "Bạn không có quyền truy cập Payment.");
    }


    // =====================================================
    // MAP RESPONSE
    // =====================================================

    private PaymentResponseDTO MapToResponse(
        PaymentTransaction payment,
        OrderModel order)
    {
        return new PaymentResponseDTO
        {
            Id =
                payment.Id,

            OrderId =
                payment.OrderId,

            OrderCode =
                order.OrderCode,

            CustomerId =
                order.CustomerId,

            ShopId =
                order.ShopId,

            Provider =
                payment.Provider,

            Method =
                payment.Method,

            Amount =
                payment.Amount,

            Status =
                payment.Status,

            ProviderTxnId =
                payment.ProviderTxnId,

            PaidAt =
                payment.PaidAt,

            CreatedAt =
                payment.CreatedAt
        };
    }


    // =====================================================
    // VALIDATE PAGINATION
    // =====================================================

    private void ValidatePagination(
        PaymentPaginationRequestDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (request.Page < 1)
        {
            throw new BadRequestException(
                "Page phải lớn hơn hoặc bằng 1.");
        }

        if (request.PageSize < 1)
        {
            throw new BadRequestException(
                "PageSize phải lớn hơn hoặc bằng 1.");
        }

        if (request.PageSize > 100)
        {
            throw new BadRequestException(
                "PageSize không được lớn hơn 100.");
        }
    }
}