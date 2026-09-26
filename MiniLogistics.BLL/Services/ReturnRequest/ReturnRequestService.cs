using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.ReturnRequest;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using ReturnRequestModel =
    MiniLogistics.DAL.Models.ReturnRequest;

namespace MiniLogistics.BLL.Services.ReturnRequest;

public class ReturnRequestService : IReturnRequestService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReturnRequestService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // CREATE
    // CUSTOMER
    // =====================================================

    public async Task<ReturnRequestResponseDTO> CreateAsync(
        long customerId,
        CreateReturnRequestDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.OrderId <= 0)
        {
            throw new BadRequestException(
                "OrderId không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BadRequestException(
                "Reason không được để trống.");
        }

        var reason = request.Reason.Trim();

        if (reason.Length > 500)
        {
            throw new BadRequestException(
                "Reason tối đa 500 ký tự.");
        }

        // =================================================
        // CHECK CUSTOMER
        // =================================================

        var customer = await _unitOfWork.Users
            .GetByIdAsync(customerId);

        if (customer == null)
        {
            throw new NotFoundException(
                $"User {customerId} không tồn tại.");
        }

        if (customer.Status != "active")
        {
            throw new BadRequestException(
                "Tài khoản Customer không hoạt động.");
        }

        // =================================================
        // GET ORDER
        // =================================================

        var order = await _unitOfWork.Orders
            .GetByIdAsync(request.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                $"Order {request.OrderId} không tồn tại.");
        }

        // =================================================
        // OWNERSHIP
        // =================================================

        if (order.CustomerId != customerId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền yêu cầu trả hàng cho Order này.");
        }

        // =================================================
        // CHECK ORDER STATUS
        // =================================================

        var allowedStatuses = new[]
        {
            "delivered",
            "completed"
        };

        if (!allowedStatuses.Contains(
                order.Status.Trim().ToLowerInvariant()))
        {
            throw new BadRequestException(
                "Chỉ có thể yêu cầu trả hàng khi Order đã được giao hoặc hoàn tất.");
        }

        // =================================================
        // CHECK DUPLICATE ACTIVE REQUEST
        // =================================================

        var existingRequests =
            await _unitOfWork.ReturnRequests.FindAsync(
                x =>
                    x.OrderId == order.Id
                    &&
                    x.CustomerId == customerId
                    &&
                    (
                        x.Status == "requested"
                        ||
                        x.Status == "approved"
                    ));

        if (existingRequests.Any())
        {
            throw new BadRequestException(
                "Order này đã có yêu cầu trả hàng đang được xử lý.");
        }

        // =================================================
        // CREATE
        // =================================================

        var returnRequest = new ReturnRequestModel
        {
            OrderId = order.Id,

            CustomerId = customerId,

            Reason = reason,

            Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),

            Status = "requested",

            RequestedAt = DateTime.UtcNow,

            HandledByUserId = null,

            HandledAt = null
        };

        await _unitOfWork.ReturnRequests
            .AddAsync(returnRequest);

        await _unitOfWork.SaveChangesAsync();

        return await BuildResponseAsync(returnRequest);
    }

    // =====================================================
    // GET MY REQUESTS
    // CUSTOMER
    // =====================================================

    public async Task<PagedResponseDTO<ReturnRequestResponseDTO>>
        GetMyRequestsAsync(
            long customerId,
            ReturnRequestPaginationRequestDTO request)
    {
        var requests =
            await _unitOfWork.ReturnRequests.FindAsync(
                x => x.CustomerId == customerId);

        return await BuildPagedResponseAsync(
            requests,
            request);
    }

    // =====================================================
    // GET BY ID
    // CUSTOMER / SELLER / ADMIN
    // =====================================================

    public async Task<ReturnRequestResponseDTO?>
        GetByIdAsync(
            long userId,
            string role,
            long id)
    {
        var request =
            await _unitOfWork.ReturnRequests
                .GetByIdAsync(id);

        if (request == null)
        {
            return null;
        }

        role = role.Trim().ToLowerInvariant();

        // =================================================
        // ADMIN
        // =================================================

        if (role == "admin")
        {
            return await BuildResponseAsync(request);
        }

        // =================================================
        // CUSTOMER
        // =================================================

        if (role == "customer")
        {
            if (request.CustomerId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem ReturnRequest này.");
            }

            return await BuildResponseAsync(request);
        }

        // =================================================
        // SELLER
        // =================================================

        if (role == "seller")
        {
            var order =
                await _unitOfWork.Orders
                    .GetByIdAsync(request.OrderId);

            if (order == null)
            {
                throw new NotFoundException(
                    "Order của ReturnRequest không tồn tại.");
            }

            var shop =
                await _unitOfWork.Shops
                    .GetByIdAsync(order.ShopId);

            if (shop == null)
            {
                throw new NotFoundException(
                    "Shop không tồn tại.");
            }

            if (shop.OwnerUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem ReturnRequest này.");
            }

            return await BuildResponseAsync(request);
        }

        throw new ForbiddenException(
            "Role không được phép xem ReturnRequest.");
    }

    // =====================================================
    // GET SHOP REQUESTS
    // SELLER
    // =====================================================

    public async Task<PagedResponseDTO<ReturnRequestResponseDTO>>
        GetShopRequestsAsync(
            long sellerId,
            ReturnRequestPaginationRequestDTO request)
    {
        var shops =
            await _unitOfWork.Shops.FindAsync(
                x => x.OwnerUserId == sellerId);

        var shopIds = shops
            .Select(x => x.Id)
            .ToHashSet();

        if (!shopIds.Any())
        {
            return CreateEmptyPagedResponse(request);
        }

        var orders =
            await _unitOfWork.Orders.FindAsync(
                x => shopIds.Contains(x.ShopId));

        var orderIds = orders
            .Select(x => x.Id)
            .ToHashSet();

        if (!orderIds.Any())
        {
            return CreateEmptyPagedResponse(request);
        }

        var requests =
            await _unitOfWork.ReturnRequests.FindAsync(
                x => orderIds.Contains(x.OrderId));

        return await BuildPagedResponseAsync(
            requests,
            request);
    }

    // =====================================================
    // GET ALL
    // ADMIN
    // =====================================================

    public async Task<PagedResponseDTO<ReturnRequestResponseDTO>>
        GetAllAsync(
            ReturnRequestPaginationRequestDTO request)
    {
        var requests =
            await _unitOfWork.ReturnRequests
                .GetAllAsync();

        return await BuildPagedResponseAsync(
            requests,
            request);
    }

    // =====================================================
    // APPROVE
    // SELLER / ADMIN
    // =====================================================

    public async Task<ReturnRequestResponseDTO>
        ApproveAsync(
            long handlerUserId,
            string role,
            long id)
    {
        var request =
            await GetRequestOrThrowAsync(id);

        await CheckHandlerAccessAsync(
            handlerUserId,
            role,
            request);

        if (request.Status != "requested")
        {
            throw new BadRequestException(
                $"Không thể approve ReturnRequest đang ở trạng thái '{request.Status}'.");
        }

        request.Status = "approved";

        request.HandledByUserId =
            handlerUserId;

        request.HandledAt =
            DateTime.UtcNow;

        _unitOfWork.ReturnRequests
            .Update(request);

        await _unitOfWork.SaveChangesAsync();

        return await BuildResponseAsync(request);
    }

    // =====================================================
    // REJECT
    // SELLER / ADMIN
    // =====================================================

    public async Task<ReturnRequestResponseDTO>
        RejectAsync(
            long handlerUserId,
            string role,
            long id)
    {
        var request =
            await GetRequestOrThrowAsync(id);

        await CheckHandlerAccessAsync(
            handlerUserId,
            role,
            request);

        if (request.Status != "requested")
        {
            throw new BadRequestException(
                $"Không thể reject ReturnRequest đang ở trạng thái '{request.Status}'.");
        }

        request.Status = "rejected";

        request.HandledByUserId =
            handlerUserId;

        request.HandledAt =
            DateTime.UtcNow;

        _unitOfWork.ReturnRequests
            .Update(request);

        await _unitOfWork.SaveChangesAsync();

        return await BuildResponseAsync(request);
    }

    // =====================================================
    // GET REQUEST OR THROW
    // =====================================================

    private async Task<ReturnRequestModel>
        GetRequestOrThrowAsync(long id)
    {
        if (id <= 0)
        {
            throw new BadRequestException(
                "ReturnRequest Id không hợp lệ.");
        }

        var request =
            await _unitOfWork.ReturnRequests
                .GetByIdAsync(id);

        if (request == null)
        {
            throw new NotFoundException(
                $"ReturnRequest {id} không tồn tại.");
        }

        return request;
    }

    // =====================================================
    // CHECK HANDLER ACCESS
    // =====================================================

    private async Task CheckHandlerAccessAsync(
        long handlerUserId,
        string role,
        ReturnRequestModel request)
    {
        role = role.Trim().ToLowerInvariant();

        // =================================================
        // ADMIN
        // =================================================

        if (role == "admin")
        {
            return;
        }

        // =================================================
        // SELLER
        // =================================================

        if (role != "seller")
        {
            throw new ForbiddenException(
                "Chỉ Seller hoặc Admin mới được xử lý ReturnRequest.");
        }

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(request.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }

        var shop =
            await _unitOfWork.Shops
                .GetByIdAsync(order.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                "Shop không tồn tại.");
        }

        if (shop.OwnerUserId != handlerUserId)
        {
            throw new ForbiddenException(
                "Seller không có quyền xử lý ReturnRequest của Shop khác.");
        }
    }

    // =====================================================
    // BUILD PAGED RESPONSE
    // =====================================================

    private async Task<PagedResponseDTO<ReturnRequestResponseDTO>>
        BuildPagedResponseAsync(
            IEnumerable<ReturnRequestModel> requests,
            ReturnRequestPaginationRequestDTO request)
    {
        ValidatePagination(request);

        var result =
            new List<ReturnRequestResponseDTO>();

        foreach (var item in requests
                     .OrderByDescending(x => x.RequestedAt))
        {
            result.Add(
                await BuildResponseAsync(item));
        }

        // =================================================
        // FILTER STATUS
        // =================================================

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status =
                request.Status.Trim()
                    .ToLowerInvariant();

            ValidateStatus(status);

            result = result
                .Where(x =>
                    x.Status != null &&
                    x.Status.Trim()
                        .ToLowerInvariant() == status)
                .ToList();
        }

        // =================================================
        // SEARCH
        // =================================================

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search =
                request.Search.Trim();

            result = result
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.OrderCode)
                     &&
                     x.OrderCode.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase))

                    ||

                    (!string.IsNullOrWhiteSpace(x.CustomerEmail)
                     &&
                     x.CustomerEmail.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase))

                    ||

                    (!string.IsNullOrWhiteSpace(x.Reason)
                     &&
                     x.Reason.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase))

                    ||

                    (!string.IsNullOrWhiteSpace(x.Description)
                     &&
                     x.Description.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase))
                )
                .ToList();
        }

        // =================================================
        // PAGINATION
        // =================================================

        var totalItems = result.Count;

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
                .Take(request.PageSize)
                .ToList();

        return new PagedResponseDTO<ReturnRequestResponseDTO>
        {
            Items = items,

            Page = request.Page,

            PageSize = request.PageSize,

            TotalItems = totalItems,

            TotalPages = totalPages
        };
    }

    // =====================================================
    // EMPTY PAGED RESPONSE
    // =====================================================

    private PagedResponseDTO<ReturnRequestResponseDTO>
        CreateEmptyPagedResponse(
            ReturnRequestPaginationRequestDTO request)
    {
        ValidatePagination(request);

        return new PagedResponseDTO<ReturnRequestResponseDTO>
        {
            Items = new List<ReturnRequestResponseDTO>(),

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
        ReturnRequestPaginationRequestDTO request)
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

    private void ValidateStatus(string status)
    {
        var allowedStatuses = new[]
        {
            "requested",
            "approved",
            "rejected"
        };

        if (!allowedStatuses.Contains(status))
        {
            throw new BadRequestException(
                "Status không hợp lệ. " +
                "Chỉ chấp nhận: requested, approved, rejected.");
        }
    }

    // =====================================================
    // MAP
    // =====================================================

    private async Task<ReturnRequestResponseDTO>
        BuildResponseAsync(
            ReturnRequestModel request)
    {
        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(request.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                $"Order {request.OrderId} không tồn tại.");
        }

        var customer =
            await _unitOfWork.Users
                .GetByIdAsync(request.CustomerId);

        if (customer == null)
        {
            throw new NotFoundException(
                $"Customer {request.CustomerId} không tồn tại.");
        }

        string? handlerEmail = null;

        if (request.HandledByUserId.HasValue)
        {
            var handler =
                await _unitOfWork.Users
                    .GetByIdAsync(
                        request.HandledByUserId.Value);

            handlerEmail = handler?.Email;
        }

        return new ReturnRequestResponseDTO
        {
            Id = request.Id,

            OrderId = request.OrderId,

            OrderCode = order.OrderCode,

            CustomerId = request.CustomerId,

            CustomerEmail = customer.Email,

            Reason = request.Reason,

            Description = request.Description,

            Status = request.Status,

            RequestedAt = request.RequestedAt,

            HandledByUserId =
                request.HandledByUserId,

            HandledByUserEmail =
                handlerEmail,

            HandledAt =
                request.HandledAt
        };
    }
}