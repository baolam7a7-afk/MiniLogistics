using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Shipment;
using MiniLogistics.BLL.Exceptions;

using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

using ShipmentModel = MiniLogistics.DAL.Models.Shipment;
using ShipmentEventModel = MiniLogistics.DAL.Models.ShipmentEvent;

namespace MiniLogistics.BLL.Services.Shipment;

public class ShipmentService : IShipmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShipmentService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // CREATE SHIPMENT
    // ADMIN / SELLER
    // =====================================================

    public async Task<ShipmentResponseDTO> CreateAsync(
        long actorUserId,
        string actorRole,
        CreateShipmentDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        actorRole =
            actorRole.Trim().ToLowerInvariant();

        if (actorRole != "admin" &&
            actorRole != "seller")
        {
            throw new ForbiddenException(
                "Bạn không có quyền tạo Shipment.");
        }

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(request.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }

        if (order.Status != "processing")
        {
            throw new BadRequestException(
                "Chỉ có thể tạo Shipment cho Order đang processing.");
        }

        var existing =
            await _unitOfWork.Shipments
                .FindAsync(
                    x =>
                        x.OrderId ==
                        order.Id);

        if (existing.Any())
        {
            throw new BadRequestException(
                "Order này đã có Shipment.");
        }

        // =================================================
        // SELLER CHỈ ĐƯỢC THAO TÁC ORDER CỦA SHOP MÌNH
        // =================================================

        if (actorRole == "seller")
        {
            bool ownsShop =
                await _unitOfWork.Shops
                    .AnyAsync(
                        x =>
                            x.Id == order.ShopId &&
                            x.OwnerUserId == actorUserId);

            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền tạo Shipment cho Order này.");
            }
        }

        // =================================================
        // CREATE SHIPMENT
        // =================================================

        var shipment =
            new ShipmentModel
            {
                OrderId =
                    order.Id,

                ShipperUserId =
                    null,

                Status =
                    "created",

                CodAmount =
                    order.PaymentMethod == "cod"
                        ? order.Total
                        : 0m,

                CreatedAt =
                    DateTime.UtcNow
            };

        await _unitOfWork.Shipments
            .AddAsync(shipment);

        await _unitOfWork.SaveChangesAsync();

        // =================================================
        // GENERATE TRACKING CODE
        // =================================================

        shipment.TrackingCode =
            $"SHP-{DateTime.UtcNow:yyyyMMddHHmmss}-{shipment.Id}";

        shipment.UpdatedAt =
            DateTime.UtcNow;

        _unitOfWork.Shipments
            .Update(shipment);

        // =================================================
        // CREATE SHIPMENT EVENT
        // =================================================

        await _unitOfWork.ShipmentEvents
            .AddAsync(
                new ShipmentEventModel
                {
                    ShipmentId =
                        shipment.Id,

                    Status =
                        "created",

                    Location =
                        null,

                    Note =
                        "Shipment được tạo.",

                    CreatedAt =
                        DateTime.UtcNow
                });

        await _unitOfWork.SaveChangesAsync();

        return await BuildResponse(
            shipment);
    }


    // =====================================================
    // ASSIGN SHIPPER
    // ADMIN / SELLER
    // =====================================================

    public async Task<ShipmentResponseDTO> AssignShipperAsync(
        long shipmentId,
        long actorUserId,
        string actorRole,
        AssignShipperDTO request)
    {
        actorRole =
            actorRole.Trim().ToLowerInvariant();

        if (actorRole != "admin" &&
            actorRole != "seller")
        {
            throw new ForbiddenException(
                "Bạn không có quyền phân công Shipper.");
        }

        if (request == null ||
            request.ShipperUserId <= 0)
        {
            throw new BadRequestException(
                "ShipperUserId không hợp lệ.");
        }

        // =================================================
        // GET SHIPMENT
        // =================================================

        var shipment =
            await _unitOfWork.Shipments
                .GetByIdAsync(shipmentId);

        if (shipment == null)
        {
            throw new NotFoundException(
                "Shipment không tồn tại.");
        }

        // =================================================
        // CHECK STATUS
        // =================================================

        if (shipment.Status == "delivered")
        {
            throw new BadRequestException(
                "Shipment đã delivered và không thể phân công lại.");
        }

        if (shipment.Status == "cancelled")
        {
            throw new BadRequestException(
                "Shipment đã cancelled.");
        }

        // =================================================
        // GET ORDER
        // =================================================

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(
                    shipment.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order của Shipment không tồn tại.");
        }

        // =================================================
        // SELLER CHECK SHOP
        // =================================================

        if (actorRole == "seller")
        {
            bool ownsShop =
                await _unitOfWork.Shops
                    .AnyAsync(
                        x =>
                            x.Id == order.ShopId &&
                            x.OwnerUserId == actorUserId);

            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền phân công Shipment này.");
            }
        }

        // =================================================
        // GET SHIPPER
        // =================================================

        var shipper =
            await _unitOfWork.Users
                .GetByIdAsync(
                    request.ShipperUserId);

        if (shipper == null)
        {
            throw new NotFoundException(
                "Shipper không tồn tại.");
        }

        // =================================================
        // CHECK SHIPPER ACTIVE
        // =================================================

        if (shipper.Status != "active")
        {
            throw new BadRequestException(
                "Tài khoản Shipper không hoạt động.");
        }

        // =================================================
        // CHECK ROLE SHIPPER
        // =================================================

        bool isShipper =
            await IsShipperUser(
                shipper.Id);

        if (!isShipper)
        {
            throw new BadRequestException(
                "User này không có role shipper.");
        }

        // =================================================
        // ASSIGN
        // =================================================

        shipment.ShipperUserId =
            shipper.Id;

        shipment.Status =
            "assigned";

        shipment.AssignedAt =
            DateTime.UtcNow;

        shipment.UpdatedAt =
            DateTime.UtcNow;

        _unitOfWork.Shipments
            .Update(shipment);

        // =================================================
        // CREATE EVENT
        // =================================================

        await _unitOfWork.ShipmentEvents
            .AddAsync(
                new ShipmentEventModel
                {
                    ShipmentId =
                        shipment.Id,

                    Status =
                        "assigned",

                    Location =
                        null,

                    Note =
                        $"Đã phân công Shipper {shipper.Id}.",

                    CreatedAt =
                        DateTime.UtcNow
                });

        await _unitOfWork.SaveChangesAsync();

        return await BuildResponse(
            shipment);
    }


    // =====================================================
    // GET ALL
    // ADMIN
    // PAGINATION
    // =====================================================

    public async Task<PagedResponseDTO<ShipmentResponseDTO>>
        GetAllAsync(
            ShipmentPaginationRequestDTO request)
    {
        ValidatePagination(request);

        // =================================================
        // GET ALL SHIPMENTS
        // =================================================

        var shipments =
            await _unitOfWork.Shipments
                .GetAllAsync();

        IEnumerable<ShipmentModel> query =
            shipments;

        // =================================================
        // SEARCH TRACKING CODE
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Search))
        {
            string search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            query =
                query.Where(
                    x =>
                        x.TrackingCode != null &&
                        x.TrackingCode
                            .ToLowerInvariant()
                            .Contains(search));
        }

        // =================================================
        // FILTER STATUS
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Status))
        {
            string status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            query =
                query.Where(
                    x =>
                        x.Status != null &&
                        x.Status
                            .ToLowerInvariant()
                            == status);
        }

        // =================================================
        // FILTER ORDER ID
        // =================================================

        if (request.OrderId.HasValue)
        {
            long orderId =
                request.OrderId.Value;

            query =
                query.Where(
                    x =>
                        x.OrderId == orderId);
        }

        // =================================================
        // FILTER SHIPPER USER ID
        // =================================================

        if (request.ShipperUserId.HasValue)
        {
            long shipperUserId =
                request.ShipperUserId.Value;

            query =
                query.Where(
                    x =>
                        x.ShipperUserId ==
                        shipperUserId);
        }

        // =================================================
        // TOTAL ITEMS
        // =================================================

        int totalItems =
            query.Count();

        // =================================================
        // SORT
        // =================================================

        query =
            query.OrderByDescending(
                x =>
                    x.CreatedAt);

        // =================================================
        // PAGINATION
        // =================================================

        var pagedShipments =
            query
                .Skip(
                    (request.Page - 1) *
                    request.PageSize)
                .Take(
                    request.PageSize)
                .ToList();

        // =================================================
        // BUILD RESPONSE
        // =================================================

        var items =
            new List<ShipmentResponseDTO>();

        foreach (var shipment
                 in pagedShipments)
        {
            items.Add(
                await BuildResponse(
                    shipment));
        }

        // =================================================
        // TOTAL PAGES
        // =================================================

        int totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)request.PageSize);

        // =================================================
        // RETURN
        // =================================================

        return new PagedResponseDTO<ShipmentResponseDTO>
        {
            Items =
                items,

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
    // GET MY SHIPMENTS
    // SHIPPER
    // PAGINATION
    // =====================================================

    public async Task<PagedResponseDTO<ShipmentResponseDTO>>
        GetMyShipmentsAsync(
            long shipperUserId,
            ShipmentPaginationRequestDTO request)
    {
        ValidatePagination(request);

        // =================================================
        // GET SHIPMENTS OF CURRENT SHIPPER
        // =================================================

        var shipments =
            await _unitOfWork.Shipments
                .FindAsync(
                    x =>
                        x.ShipperUserId ==
                        shipperUserId);

        IEnumerable<ShipmentModel> query =
            shipments;

        // =================================================
        // SEARCH TRACKING CODE
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Search))
        {
            string search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            query =
                query.Where(
                    x =>
                        x.TrackingCode != null &&
                        x.TrackingCode
                            .ToLowerInvariant()
                            .Contains(search));
        }

        // =================================================
        // FILTER STATUS
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Status))
        {
            string status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            query =
                query.Where(
                    x =>
                        x.Status != null &&
                        x.Status
                            .ToLowerInvariant()
                            == status);
        }

        // =================================================
        // FILTER ORDER ID
        // =================================================

        if (request.OrderId.HasValue)
        {
            long orderId =
                request.OrderId.Value;

            query =
                query.Where(
                    x =>
                        x.OrderId ==
                        orderId);
        }

        // =================================================
        // TOTAL ITEMS
        // =================================================

        int totalItems =
            query.Count();

        // =================================================
        // SORT
        // =================================================

        query =
            query.OrderByDescending(
                x =>
                    x.CreatedAt);

        // =================================================
        // PAGINATION
        // =================================================

        var pagedShipments =
            query
                .Skip(
                    (request.Page - 1) *
                    request.PageSize)
                .Take(
                    request.PageSize)
                .ToList();

        // =================================================
        // BUILD RESPONSE
        // =================================================

        var items =
            new List<ShipmentResponseDTO>();

        foreach (var shipment
                 in pagedShipments)
        {
            items.Add(
                await BuildResponse(
                    shipment));
        }

        // =================================================
        // TOTAL PAGES
        // =================================================

        int totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)request.PageSize);

        // =================================================
        // RETURN
        // =================================================

        return new PagedResponseDTO<ShipmentResponseDTO>
        {
            Items =
                items,

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
    // ADMIN / SELLER / SHIPPER
    // =====================================================

    public async Task<ShipmentResponseDTO>
        GetByIdAsync(
            long shipmentId,
            long userId,
            string role)
    {
        role =
            role.Trim()
                .ToLowerInvariant();

        // =================================================
        // GET SHIPMENT
        // =================================================

        var shipment =
            await _unitOfWork.Shipments
                .GetByIdAsync(
                    shipmentId);

        if (shipment == null)
        {
            throw new NotFoundException(
                "Shipment không tồn tại.");
        }

        // =================================================
        // SHIPPER
        // CHỈ ĐƯỢC XEM SHIPMENT CỦA MÌNH
        // =================================================

        if (role == "shipper" &&
            shipment.ShipperUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem Shipment này.");
        }

        // =================================================
        // SELLER
        // CHỈ ĐƯỢC XEM SHOP CỦA MÌNH
        // =================================================

        if (role == "seller")
        {
            var order =
                await _unitOfWork.Orders
                    .GetByIdAsync(
                        shipment.OrderId);

            if (order == null)
            {
                throw new NotFoundException(
                    "Order không tồn tại.");
            }

            bool ownsShop =
                await _unitOfWork.Shops
                    .AnyAsync(
                        x =>
                            x.Id ==
                                order.ShopId &&
                            x.OwnerUserId ==
                                userId);

            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Shipment này.");
            }
        }

        return await BuildResponse(
            shipment);
    }


    // =====================================================
    // UPDATE SHIPMENT STATUS
    // SHIPPER
    // =====================================================

    public async Task<ShipmentResponseDTO>
        UpdateStatusAsync(
            long shipmentId,
            long shipperUserId,
            UpdateShipmentStatusDTO request)
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

        // =================================================
        // TRANSACTION
        // =================================================

        return await _unitOfWork
            .ExecuteInTransactionAsync(
                async () =>
                {
                    // =====================================
                    // GET SHIPMENT
                    // =====================================

                    var shipment =
                        await _unitOfWork.Shipments
                            .GetByIdAsync(
                                shipmentId);

                    if (shipment == null)
                    {
                        throw new NotFoundException(
                            "Shipment không tồn tại.");
                    }

                    // =====================================
                    // CHECK OWNERSHIP
                    // =====================================

                    if (shipment.ShipperUserId !=
                        shipperUserId)
                    {
                        throw new ForbiddenException(
                            "Bạn không được thao tác Shipment này.");
                    }

                    // =====================================
                    // NORMALIZE STATUS
                    // =====================================

                    string newStatus =
                        request.Status
                            .Trim()
                            .ToLowerInvariant();

                    string oldShipmentStatus =
                        shipment.Status
                            .Trim()
                            .ToLowerInvariant();

                    // =====================================
                    // VALIDATE TRANSITION
                    // =====================================

                    ValidateStatusTransition(
                        oldShipmentStatus,
                        newStatus);

                    // =====================================
                    // UPDATE SHIPMENT STATUS
                    // =====================================

                    shipment.Status =
                        newStatus;

                    shipment.UpdatedAt =
                        DateTime.UtcNow;

                    // =====================================
                    // PICKED UP
                    // =====================================

                    if (newStatus ==
                        "picked_up")
                    {
                        shipment.PickedAt =
                            DateTime.UtcNow;
                    }

                    // =====================================
                    // DELIVERED
                    // =====================================

                    if (newStatus ==
                        "delivered")
                    {
                        shipment.DeliveredAt =
                            DateTime.UtcNow;

                        // =================================
                        // GET ORDER
                        // =================================

                        var order =
                            await _unitOfWork.Orders
                                .GetByIdAsync(
                                    shipment.OrderId);

                        if (order == null)
                        {
                            throw new NotFoundException(
                                "Order của Shipment không tồn tại.");
                        }

                        // =================================
                        // ORDER MUST BE PROCESSING
                        // =================================

                        string oldOrderStatus =
                            order.Status
                                .Trim()
                                .ToLowerInvariant();

                        if (!string.Equals(
                                oldOrderStatus,
                                "processing",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new BadRequestException(
                                $"Order hiện tại đang ở trạng thái " +
                                $"'{order.Status}', " +
                                "không thể chuyển sang delivered.");
                        }

                        // =================================
                        // UPDATE ORDER
                        // =================================

                        order.Status =
                            "delivered";

                        order.UpdatedAt =
                            DateTime.UtcNow;

                        _unitOfWork.Orders
                            .Update(order);

                        // =================================
                        // ORDER STATUS LOG
                        // =================================

                        await _unitOfWork
                            .OrderStatusLogs
                            .AddAsync(
                                new OrderStatusLog
                                {
                                    OrderId =
                                        order.Id,

                                    FromStatus =
                                        oldOrderStatus,

                                    ToStatus =
                                        "delivered",

                                    Message =
                                        "Shipment đã giao hàng thành công.",

                                    CreatedByUserId =
                                        shipperUserId,

                                    CreatedAt =
                                        DateTime.UtcNow
                                });

                        // =================================
                        // COD PAYMENT
                        // =================================

                        if (string.Equals(
                                order.PaymentMethod,
                                "cod",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            var payments =
                                await _unitOfWork
                                    .PaymentTransactions
                                    .FindAsync(
                                        x =>
                                            x.OrderId ==
                                            order.Id);

                            var payment =
                                payments
                                    .OrderByDescending(
                                        x =>
                                            x.CreatedAt)
                                    .FirstOrDefault();

                            // =============================
                            // NO PAYMENT
                            // =============================

                            if (payment == null)
                            {
                                payment =
                                    new PaymentTransaction
                                    {
                                        OrderId =
                                            order.Id,

                                        Provider =
                                            null,

                                        Method =
                                            "cod",

                                        Amount =
                                            order.Total,

                                        Status =
                                            "paid",

                                        ProviderTxnId =
                                            null,

                                        PaidAt =
                                            DateTime.UtcNow,

                                        CreatedAt =
                                            DateTime.UtcNow
                                    };

                                await _unitOfWork
                                    .PaymentTransactions
                                    .AddAsync(
                                        payment);
                            }

                            // =============================
                            // PENDING → PAID
                            // =============================

                            else if (string.Equals(
                                payment.Status,
                                "pending",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                payment.Status =
                                    "paid";

                                payment.PaidAt =
                                    DateTime.UtcNow;

                                _unitOfWork
                                    .PaymentTransactions
                                    .Update(
                                        payment);
                            }
                        }
                    }

                    // =====================================
                    // UPDATE SHIPMENT
                    // =====================================

                    _unitOfWork.Shipments
                        .Update(shipment);

                    // =====================================
                    // SHIPMENT EVENT
                    // =====================================

                    await _unitOfWork
                        .ShipmentEvents
                        .AddAsync(
                            new ShipmentEventModel
                            {
                                ShipmentId =
                                    shipment.Id,

                                Status =
                                    newStatus,

                                Location =
                                    string.IsNullOrWhiteSpace(
                                        request.Location)
                                        ? null
                                        : request.Location
                                            .Trim(),

                                Note =
                                    string.IsNullOrWhiteSpace(
                                        request.Note)
                                        ? null
                                        : request.Note
                                            .Trim(),

                                CreatedAt =
                                    DateTime.UtcNow
                            });

                    // =====================================
                    // SAVE
                    // =====================================

                    await _unitOfWork
                        .SaveChangesAsync();

                    return await BuildResponse(
                        shipment);
                });
    }


    // =====================================================
    // CHECK USER HAS SHIPPER ROLE
    // =====================================================

    private async Task<bool>
        IsShipperUser(
            long userId)
    {
        var userRoles =
            await _unitOfWork.UserRoles
                .FindAsync(
                    x =>
                        x.UserId ==
                        userId);

        foreach (var userRole
                 in userRoles)
        {
            var role =
                await _unitOfWork.Roles
                    .GetByIdAsync(
                        userRole.RoleId);

            if (role != null &&
                string.Equals(
                    role.Name,
                    "shipper",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }


    // =====================================================
    // STATUS TRANSITION
    // =====================================================

    private void ValidateStatusTransition(
        string currentStatus,
        string newStatus)
    {
        currentStatus =
            currentStatus
                .Trim()
                .ToLowerInvariant();

        newStatus =
            newStatus
                .Trim()
                .ToLowerInvariant();

        // =================================================
        // CREATED → ASSIGNED
        // =================================================

        if (currentStatus ==
                "created" &&
            newStatus ==
                "assigned")
        {
            return;
        }

        // =================================================
        // ASSIGNED → PICKED_UP
        // =================================================

        if (currentStatus ==
                "assigned" &&
            newStatus ==
                "picked_up")
        {
            return;
        }

        // =================================================
        // PICKED_UP → SHIPPING
        // =================================================

        if (currentStatus ==
                "picked_up" &&
            newStatus ==
                "shipping")
        {
            return;
        }

        // =================================================
        // SHIPPING → DELIVERED
        // =================================================

        if (currentStatus ==
                "shipping" &&
            newStatus ==
                "delivered")
        {
            return;
        }

        throw new BadRequestException(
            $"Không thể chuyển Shipment từ " +
            $"'{currentStatus}' sang '{newStatus}'.");
    }


    // =====================================================
    // VALIDATE PAGINATION
    // =====================================================

    private void ValidatePagination(
        ShipmentPaginationRequestDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Pagination request không được null.");
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


    // =====================================================
    // BUILD RESPONSE
    // =====================================================

    private async Task<ShipmentResponseDTO>
        BuildResponse(
            ShipmentModel shipment)
    {
        string? shipperName = null;

        // =================================================
        // GET SHIPPER NAME
        // =================================================

        if (shipment.ShipperUserId.HasValue)
        {
            var shipper =
                await _unitOfWork.Users
                    .GetByIdAsync(
                        shipment.ShipperUserId.Value);

            shipperName =
                shipper?.FullName;
        }

        // =================================================
        // GET EVENTS
        // =================================================

        var events =
            await _unitOfWork.ShipmentEvents
                .FindAsync(
                    x =>
                        x.ShipmentId ==
                        shipment.Id);

        // =================================================
        // BUILD RESPONSE
        // =================================================

        return new ShipmentResponseDTO
        {
            Id =
                shipment.Id,

            OrderId =
                shipment.OrderId,

            ShipperUserId =
                shipment.ShipperUserId,

            ShipperName =
                shipperName,

            TrackingCode =
                shipment.TrackingCode,

            Status =
                shipment.Status,

            CodAmount =
                shipment.CodAmount,

            AssignedAt =
                shipment.AssignedAt,

            PickedAt =
                shipment.PickedAt,

            DeliveredAt =
                shipment.DeliveredAt,

            CreatedAt =
                shipment.CreatedAt,

            UpdatedAt =
                shipment.UpdatedAt,

            Events =
                events
                    .OrderBy(
                        x =>
                            x.CreatedAt)
                    .Select(
                        x =>
                            new ShipmentEventResponseDTO
                            {
                                Id =
                                    x.Id,

                                Status =
                                    x.Status,

                                Location =
                                    x.Location,

                                Note =
                                    x.Note,

                                CreatedAt =
                                    x.CreatedAt
                            })
                    .ToList()
        };
    }
}