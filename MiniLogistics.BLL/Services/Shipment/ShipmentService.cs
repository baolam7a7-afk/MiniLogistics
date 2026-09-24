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

    public ShipmentService(IUnitOfWork unitOfWork)
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
                .FindAsync(x =>
                    x.OrderId == order.Id);

        if (existing.Any())
        {
            throw new BadRequestException(
                "Order này đã có Shipment.");
        }

        // Seller chỉ được thao tác Order của Shop mình
        if (actorRole == "seller")
        {
            bool ownsShop =
                await _unitOfWork.Shops
                    .AnyAsync(x =>
                        x.Id == order.ShopId &&
                        x.OwnerUserId == actorUserId);

            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền tạo Shipment cho Order này.");
            }
        }

        var shipment = new ShipmentModel
        {
            OrderId = order.Id,

            ShipperUserId = null,

            Status = "created",

            CodAmount =
                order.PaymentMethod == "cod"
                    ? order.Total
                    : 0m,

            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Shipments
            .AddAsync(shipment);

        await _unitOfWork.SaveChangesAsync();

        shipment.TrackingCode =
            $"SHP-{DateTime.UtcNow:yyyyMMddHHmmss}-{shipment.Id}";

        shipment.UpdatedAt =
            DateTime.UtcNow;

        _unitOfWork.Shipments
            .Update(shipment);

        await _unitOfWork.ShipmentEvents
            .AddAsync(
                new ShipmentEventModel
                {
                    ShipmentId = shipment.Id,

                    Status = "created",

                    Location = null,

                    Note = "Shipment được tạo.",

                    CreatedAt = DateTime.UtcNow
                });

        await _unitOfWork.SaveChangesAsync();

        return await BuildResponse(shipment);
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

        var shipment =
            await _unitOfWork.Shipments
                .GetByIdAsync(shipmentId);

        if (shipment == null)
        {
            throw new NotFoundException(
                "Shipment không tồn tại.");
        }

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

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(shipment.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order của Shipment không tồn tại.");
        }

        // Seller chỉ được assign Shipment thuộc Shop của mình
        if (actorRole == "seller")
        {
            bool ownsShop =
                await _unitOfWork.Shops
                    .AnyAsync(x =>
                        x.Id == order.ShopId &&
                        x.OwnerUserId == actorUserId);

            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền phân công Shipment này.");
            }
        }

        var shipper =
            await _unitOfWork.Users
                .GetByIdAsync(request.ShipperUserId);

        if (shipper == null)
        {
            throw new NotFoundException(
                "Shipper không tồn tại.");
        }

        if (shipper.Status != "active")
        {
            throw new BadRequestException(
                "Tài khoản Shipper không hoạt động.");
        }

        bool isShipper =
            await IsShipperUser(shipper.Id);

        if (!isShipper)
        {
            throw new BadRequestException(
                "User này không có role shipper.");
        }

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

        await _unitOfWork.ShipmentEvents
            .AddAsync(
                new ShipmentEventModel
                {
                    ShipmentId =
                        shipment.Id,

                    Status =
                        "assigned",

                    Location = null,

                    Note =
                        $"Đã phân công Shipper {shipper.Id}.",

                    CreatedAt =
                        DateTime.UtcNow
                });

        await _unitOfWork.SaveChangesAsync();

        return await BuildResponse(shipment);
    }

    // =====================================================
    // GET ALL
    // ADMIN
    // =====================================================

    public async Task<List<ShipmentResponseDTO>>
        GetAllAsync()
    {
        var shipments =
            await _unitOfWork.Shipments
                .GetAllAsync();

        var result =
            new List<ShipmentResponseDTO>();

        foreach (var shipment in shipments)
        {
            result.Add(
                await BuildResponse(shipment));
        }

        return result;
    }

    // =====================================================
    // GET MY SHIPMENTS
    // SHIPPER
    // =====================================================

    public async Task<List<ShipmentResponseDTO>>
        GetMyShipmentsAsync(long shipperUserId)
    {
        var shipments =
            await _unitOfWork.Shipments
                .FindAsync(x =>
                    x.ShipperUserId == shipperUserId);

        var result =
            new List<ShipmentResponseDTO>();

        foreach (var shipment in shipments)
        {
            result.Add(
                await BuildResponse(shipment));
        }

        return result;
    }

    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<ShipmentResponseDTO>
        GetByIdAsync(
            long shipmentId,
            long userId,
            string role)
    {
        role =
            role.Trim().ToLowerInvariant();

        var shipment =
            await _unitOfWork.Shipments
                .GetByIdAsync(shipmentId);

        if (shipment == null)
        {
            throw new NotFoundException(
                "Shipment không tồn tại.");
        }

        // Shipper chỉ được xem Shipment của mình
        if (role == "shipper" &&
            shipment.ShipperUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem Shipment này.");
        }

        // Seller chỉ được xem Shipment của Shop mình
        if (role == "seller")
        {
            var order =
                await _unitOfWork.Orders
                    .GetByIdAsync(shipment.OrderId);

            if (order == null)
            {
                throw new NotFoundException(
                    "Order không tồn tại.");
            }

            bool ownsShop =
                await _unitOfWork.Shops
                    .AnyAsync(x =>
                        x.Id == order.ShopId &&
                        x.OwnerUserId == userId);

            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Shipment này.");
            }
        }

        return await BuildResponse(shipment);
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

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new BadRequestException(
                "Status không được để trống.");
        }

        // =================================================
        // TOÀN BỘ LUỒNG UPDATE STATUS NẰM TRONG TRANSACTION
        // =================================================

        return await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                // =========================================
                // GET SHIPMENT
                // =========================================

                var shipment =
                    await _unitOfWork.Shipments
                        .GetByIdAsync(shipmentId);

                if (shipment == null)
                {
                    throw new NotFoundException(
                        "Shipment không tồn tại.");
                }

                // =========================================
                // CHECK SHIPPER OWNERSHIP
                // =========================================

                if (shipment.ShipperUserId != shipperUserId)
                {
                    throw new ForbiddenException(
                        "Bạn không được thao tác Shipment này.");
                }

                // =========================================
                // NORMALIZE STATUS
                // =========================================

                string newStatus =
                    request.Status
                        .Trim()
                        .ToLowerInvariant();

                string oldShipmentStatus =
                    shipment.Status
                        .Trim()
                        .ToLowerInvariant();

                // =========================================
                // VALIDATE TRANSITION
                // =========================================

                ValidateStatusTransition(
                    oldShipmentStatus,
                    newStatus);

                // =========================================
                // UPDATE SHIPMENT STATUS
                // =========================================

                shipment.Status =
                    newStatus;

                shipment.UpdatedAt =
                    DateTime.UtcNow;

                // =========================================
                // PICKED UP
                // =========================================

                if (newStatus == "picked_up")
                {
                    shipment.PickedAt =
                        DateTime.UtcNow;
                }

                // =========================================
                // DELIVERED
                // =========================================

                if (newStatus == "delivered")
                {
                    shipment.DeliveredAt =
                        DateTime.UtcNow;

                    // =====================================
                    // GET ORDER
                    // =====================================

                    var order =
                        await _unitOfWork.Orders
                            .GetByIdAsync(
                                shipment.OrderId);

                    if (order == null)
                    {
                        throw new NotFoundException(
                            "Order của Shipment không tồn tại.");
                    }

                    // =====================================
                    // ORDER MUST BE PROCESSING
                    // =====================================

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

                    // =====================================
                    // UPDATE ORDER
                    // =====================================

                    order.Status =
                        "delivered";

                    order.UpdatedAt =
                        DateTime.UtcNow;

                    _unitOfWork.Orders
                        .Update(order);

                    // =====================================
                    // ORDER STATUS LOG
                    // =====================================

                    await _unitOfWork.OrderStatusLogs
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

                    // =====================================
                    // COD PAYMENT
                    // =====================================

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
                                    x => x.CreatedAt)
                                .FirstOrDefault();

                        // =================================
                        // OLD ORDER WITHOUT PAYMENT
                        // =================================

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
                                .AddAsync(payment);
                        }

                        // =================================
                        // PENDING → PAID
                        // =================================

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
                                .Update(payment);
                        }
                    }
                }

                // =========================================
                // UPDATE SHIPMENT
                // =========================================

                _unitOfWork.Shipments
                    .Update(shipment);

                // =========================================
                // SHIPMENT EVENT
                // =========================================

                await _unitOfWork.ShipmentEvents
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
                                    : request.Location.Trim(),

                            Note =
                                string.IsNullOrWhiteSpace(
                                    request.Note)
                                    ? null
                                    : request.Note.Trim(),

                            CreatedAt =
                                DateTime.UtcNow
                        });

                // =========================================
                // SAVE
                // =========================================
                //
                // Save ở đây để BuildResponse có thể đọc
                // ShipmentEvent vừa tạo.
                //
                // ExecuteInTransactionAsync bên ngoài sẽ
                // SaveChanges thêm một lần nữa trước Commit.
                // =========================================

                await _unitOfWork.SaveChangesAsync();

                return await BuildResponse(shipment);
            });
    }

    // =====================================================
    // CHECK USER HAS SHIPPER ROLE
    // =====================================================

    private async Task<bool> IsShipperUser(long userId)
    {
        var userRoles =
            await _unitOfWork.UserRoles
                .FindAsync(
                    x => x.UserId == userId);

        foreach (var userRole in userRoles)
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

        // created → assigned
        if (currentStatus == "created" &&
            newStatus == "assigned")
        {
            return;
        }

        // assigned → picked_up
        if (currentStatus == "assigned" &&
            newStatus == "picked_up")
        {
            return;
        }

        // picked_up → shipping
        if (currentStatus == "picked_up" &&
            newStatus == "shipping")
        {
            return;
        }

        // shipping → delivered
        if (currentStatus == "shipping" &&
            newStatus == "delivered")
        {
            return;
        }

        throw new BadRequestException(
            $"Không thể chuyển Shipment từ " +
            $"'{currentStatus}' sang '{newStatus}'.");
    }

    // =====================================================
    // BUILD RESPONSE
    // =====================================================

    private async Task<ShipmentResponseDTO>
        BuildResponse(ShipmentModel shipment)
    {
        string? shipperName = null;

        if (shipment.ShipperUserId.HasValue)
        {
            var shipper =
                await _unitOfWork.Users
                    .GetByIdAsync(
                        shipment.ShipperUserId.Value);

            shipperName =
                shipper?.FullName;
        }

        var events =
            await _unitOfWork.ShipmentEvents
                .FindAsync(x =>
                    x.ShipmentId == shipment.Id);

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
                    .OrderBy(x => x.CreatedAt)
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