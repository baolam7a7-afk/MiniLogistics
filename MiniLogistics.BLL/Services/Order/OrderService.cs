using Microsoft.EntityFrameworkCore;
using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Order;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

using OrderModel = MiniLogistics.DAL.Models.Order;
using OrderItemModel = MiniLogistics.DAL.Models.OrderItem;
using OrderStatusLogModel = MiniLogistics.DAL.Models.OrderStatusLog;
using ShipmentModel = MiniLogistics.DAL.Models.Shipment;
using AddressModel = MiniLogistics.DAL.Models.Address;

namespace MiniLogistics.BLL.Services.Order;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    private const decimal ShippingFee = 0m;


    public OrderService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // CREATE ORDER
    // =====================================================

    public async Task<OrderResponseDTO> CreateAsync(
        long customerId,
        CreateOrderDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (request.Items == null ||
            request.Items.Count == 0)
        {
            throw new BadRequestException(
                "Order phải có ít nhất một sản phẩm.");
        }


        // ================================================
        // PAYMENT METHOD
        // ================================================

        string paymentMethod =
            string.IsNullOrWhiteSpace(request.PaymentMethod)
                ? "cod"
                : request.PaymentMethod
                    .Trim()
                    .ToLowerInvariant();

        if (paymentMethod is not ("cod" or "qr"))
        {
            throw new BadRequestException(
                "Hiện tại Order hỗ trợ COD hoặc thanh toán QR.");
        }


        // ================================================
        // CHECK CUSTOMER
        // ================================================

        var customer =
            await _unitOfWork.Users.GetByIdAsync(
                customerId);

        if (customer == null)
        {
            throw new NotFoundException(
                "Customer không tồn tại.");
        }

        if (customer.Status != "active")
        {
            throw new BadRequestException(
                "Tài khoản Customer không hoạt động.");
        }


        // ================================================
        // CHECK ADDRESS
        // ================================================

        var address =
            await _unitOfWork.Addresses.GetByIdAsync(
                request.ShippingAddressId);

        if (address == null)
        {
            throw new NotFoundException(
                "Địa chỉ giao hàng không tồn tại.");
        }

        if (address.UserId != customerId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền sử dụng địa chỉ này.");
        }


        // ================================================
        // CHECK DUPLICATE VARIANT
        // ================================================

        var duplicateVariant =
            request.Items
                .GroupBy(x => x.VariantId)
                .Any(g => g.Count() > 1);

        if (duplicateVariant)
        {
            throw new BadRequestException(
                "Không được có cùng ProductVariant nhiều lần trong Order.");
        }


        // ================================================
        // TRANSACTION
        // ================================================

        return await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await CancelUnpaidCheckoutsAsync(customerId);

                var orderItems =
                    new List<OrderItemModel>();

                decimal subtotal = 0;

                long? shopId = null;


                // ========================================
                // PROCESS ITEMS
                // ========================================

                foreach (var requestItem in request.Items)
                {
                    if (requestItem.Quantity <= 0)
                    {
                        throw new BadRequestException(
                            "Quantity phải lớn hơn 0.");
                    }


                    // ------------------------------------
                    // GET VARIANT
                    // ------------------------------------

                    var variant =
                        await _unitOfWork.ProductVariants
                            .GetByIdAsync(
                                requestItem.VariantId);

                    if (variant == null)
                    {
                        throw new NotFoundException(
                            $"ProductVariant {requestItem.VariantId} không tồn tại.");
                    }


                    // ------------------------------------
                    // CHECK ACTIVE
                    // ------------------------------------

                    if (!variant.IsActive)
                    {
                        throw new BadRequestException(
                            $"ProductVariant {variant.Id} hiện không hoạt động.");
                    }


                    // ------------------------------------
                    // GET PRODUCT
                    // ------------------------------------

                    var product =
                        await _unitOfWork.Products
                            .GetByIdAsync(
                                variant.ProductId);

                    if (product == null)
                    {
                        throw new NotFoundException(
                            $"Product {variant.ProductId} không tồn tại.");
                    }


                    // ------------------------------------
                    // CHECK PRODUCT ACTIVE
                    // ------------------------------------

                    if (product.Status != "active")
                    {
                        throw new BadRequestException(
                            $"Product {product.Id} hiện không được bán.");
                    }


                    // ------------------------------------
                    // SHOP
                    // ------------------------------------

                    if (shopId == null)
                    {
                        shopId = product.ShopId;
                    }
                    else if (shopId.Value != product.ShopId)
                    {
                        throw new BadRequestException(
                            "Một Order hiện tại chỉ được chứa sản phẩm của cùng một Shop.");
                    }


                    // ------------------------------------
                    // CALCULATE
                    // ------------------------------------

                    decimal lineTotal =
                        variant.Price *
                        requestItem.Quantity;

                    subtotal += lineTotal;


                    // ------------------------------------
                    // ORDER ITEM
                    // ------------------------------------

                    var orderItem =
                        new OrderItemModel
                        {
                            ProductId =
                                product.Id,

                            VariantId =
                                variant.Id,

                            ProductNameSnapshot =
                                product.Name,

                            VariantNameSnapshot =
                                variant.VariantName,

                            UnitPrice =
                                variant.Price,

                            Quantity =
                                requestItem.Quantity,

                            LineTotal =
                                lineTotal
                        };

                    orderItems.Add(orderItem);
                }


                // ========================================
                // SHOP MUST EXIST
                // ========================================

                if (shopId == null)
                {
                    throw new BadRequestException(
                        "Không xác định được Shop.");
                }

                var shop =
                    await _unitOfWork.Shops
                        .GetByIdAsync(
                            shopId.Value);

                if (shop == null)
                {
                    throw new NotFoundException(
                        "Shop không tồn tại.");
                }


                // ========================================
                // RESERVE INVENTORY
                // ========================================

                foreach (var item in orderItems)
                {
                    await ReserveInventory(
                        item.VariantId,
                        item.Quantity);
                }


                // ========================================
                // DISCOUNT
                // ========================================

                decimal discountTotal = 0m;


                // ========================================
                // TOTAL
                // ========================================

                decimal total =
                    subtotal
                    + ShippingFee
                    - discountTotal;


                // ========================================
                // CREATE ORDER
                // ========================================

                var order =
                    new OrderModel
                    {
                        OrderCode =
                            GenerateOrderCode(),

                        CustomerId =
                            customerId,

                        ShopId =
                            shopId.Value,

                        ShippingAddressId =
                            request.ShippingAddressId,

                        Status =
                            paymentMethod == "qr"
                                ? "awaiting_payment"
                                : OrderStatuses.Pending,

                        Currency =
                            "VND",

                        Subtotal =
                            subtotal,

                        ShippingFee =
                            ShippingFee,

                        DiscountTotal =
                            discountTotal,

                        Total =
                            total,

                        PaymentMethod =
                            paymentMethod,

                        Note =
                            string.IsNullOrWhiteSpace(
                                request.Note)
                                ? null
                                : request.Note.Trim(),

                        PlacedAt =
                            DateTime.UtcNow,

                        UpdatedAt =
                            null
                    };


                // ========================================
                // ADD ORDER
                // ========================================

                await _unitOfWork.Orders
                    .AddAsync(order);

                await _unitOfWork.SaveChangesAsync();


                // ========================================
                // ADD ORDER ITEMS
                // ========================================

                foreach (var item in orderItems)
                {
                    item.OrderId =
                        order.Id;

                    await _unitOfWork.OrderItems
                        .AddAsync(item);
                }


                // ========================================
                // ADD PAYMENT
                // ========================================

                var payment =
                    new PaymentTransaction
                    {
                        OrderId =
                            order.Id,

                        Provider =
                            null,

                        Method =
                            paymentMethod,

                        Amount =
                            order.Total,

                        Status =
                            "pending",

                        ProviderTxnId =
                            null,

                        PaidAt =
                            null,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                await _unitOfWork.PaymentTransactions
                    .AddAsync(payment);


                // ========================================
                // STATUS LOG
                // ========================================

                var statusLog =
                    new OrderStatusLogModel
                    {
                        OrderId =
                            order.Id,

                        FromStatus =
                            null,

                        ToStatus =
                            order.Status,

                        Message =
                            "Order được tạo.",

                        CreatedByUserId =
                            customerId,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                await _unitOfWork.OrderStatusLogs
                    .AddAsync(statusLog);

                if (paymentMethod != "qr")
                {
                    await RemoveOrderedCartItems(
                        customerId,
                        orderItems);
                }


                // ========================================
                // SAVE ALL
                // ========================================

                await _unitOfWork.SaveChangesAsync();


                // ========================================
                // RESPONSE
                // ========================================

                return await BuildResponse(order);
            });
    }


    // =====================================================
    // GET MY ORDERS - CUSTOMER
    // PAGINATION
    // =====================================================

    public async Task<PagedResponseDTO<OrderResponseDTO>>
        GetMyOrdersAsync(
            long customerId,
            OrderPaginationRequestDTO request)
    {
        ValidatePagination(request);


        var orders =
            await _unitOfWork.Orders
                .FindAsync(
                    x =>
                        x.CustomerId ==
                        customerId);


        // ================================================
        // FILTER STATUS
        // ================================================

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            string status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            orders =
                orders
                    .Where(x =>
                        MatchesCustomerStatusFilter(
                            x.Status,
                            status))
                    .ToList();
        }


        // ================================================
        // SEARCH ORDER CODE
        // ================================================

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            orders =
                orders
                    .Where(x =>
                        x.OrderCode
                            .ToLower()
                            .Contains(search))
                    .ToList();
        }


        // ================================================
        // ORDER BY
        // ================================================

        var orderedOrders =
            orders
                .OrderByDescending(
                    x => x.PlacedAt)
                .ToList();


        // ================================================
        // PAGINATION
        // ================================================

        int totalItems =
            orderedOrders.Count;

        int totalPages =
            (int)Math.Ceiling(
                totalItems /
                (double)request.PageSize);

        var pagedOrders =
            orderedOrders
                .Skip(
                    (request.Page - 1)
                    * request.PageSize)
                .Take(request.PageSize)
                .ToList();


        // ================================================
        // BUILD RESPONSE
        // ================================================

        var result =
            new List<OrderResponseDTO>();

        foreach (var order in pagedOrders)
        {
            result.Add(
                await BuildResponse(order));
        }


        return new PagedResponseDTO<OrderResponseDTO>
        {
            Items =
                result,

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

    public async Task<OrderResponseDTO>
        GetByIdAsync(
            long orderId,
            long userId,
            string role)
    {
        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(
                    orderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }

        await CheckOrderAccess(
            order,
            userId,
            role);

        return await BuildResponse(order);
    }


    // =====================================================
    // GET ALL - ADMIN
    // PAGINATION
    // =====================================================

    public async Task<PagedResponseDTO<OrderResponseDTO>>
        GetAllAsync(
            OrderPaginationRequestDTO request)
    {
        ValidatePagination(request);


        var orders =
            await _unitOfWork.Orders
                .GetAllAsync();


        // ================================================
        // FILTER STATUS
        // ================================================

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            string status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            orders =
                orders
                    .Where(x =>
                        x.Status.ToLower() ==
                        status)
                    .ToList();
        }


        // ================================================
        // SEARCH ORDER CODE
        // ================================================

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            orders =
                orders
                    .Where(x =>
                        x.OrderCode
                            .ToLower()
                            .Contains(search))
                    .ToList();
        }


        // ================================================
        // ORDER BY
        // ================================================

        var orderedOrders =
            orders
                .OrderByDescending(
                    x => x.PlacedAt)
                .ToList();


        int totalItems =
            orderedOrders.Count;

        int totalPages =
            (int)Math.Ceiling(
                totalItems /
                (double)request.PageSize);


        var pagedOrders =
            orderedOrders
                .Skip(
                    (request.Page - 1)
                    * request.PageSize)
                .Take(request.PageSize)
                .ToList();


        var result =
            new List<OrderResponseDTO>();

        foreach (var order in pagedOrders)
        {
            result.Add(
                await BuildResponse(order));
        }


        return new PagedResponseDTO<OrderResponseDTO>
        {
            Items =
                result,

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
    // GET BY SHOP OWNER - SELLER
    // PAGINATION
    // =====================================================

    public async Task<PagedResponseDTO<OrderResponseDTO>>
        GetByShopOwnerAsync(
            long sellerUserId,
            OrderPaginationRequestDTO request)
    {
        ValidatePagination(request);


        // ================================================
        // GET SELLER SHOPS
        // ================================================

        var shops =
            await _unitOfWork.Shops
                .FindAsync(
                    x =>
                        x.OwnerUserId ==
                        sellerUserId);

        var shopIds =
            shops
                .Select(x => x.Id)
                .ToHashSet();


        // ================================================
        // GET ORDERS
        // ================================================

        var orders =
            await _unitOfWork.Orders
                .GetAllAsync();

        orders =
            orders
                .Where(x =>
                    shopIds.Contains(
                        x.ShopId))
                .ToList();


        // ================================================
        // FILTER STATUS
        // ================================================

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            string status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            orders =
                orders
                    .Where(x =>
                        x.Status.ToLower() ==
                        status)
                    .ToList();
        }


        // ================================================
        // SEARCH ORDER CODE
        // ================================================

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            orders =
                orders
                    .Where(x =>
                        x.OrderCode
                            .ToLower()
                            .Contains(search))
                    .ToList();
        }


        // ================================================
        // ORDER BY
        // ================================================

        var orderedOrders =
            orders
                .OrderByDescending(
                    x => x.PlacedAt)
                .ToList();


        int totalItems =
            orderedOrders.Count;

        int totalPages =
            (int)Math.Ceiling(
                totalItems /
                (double)request.PageSize);


        var pagedOrders =
            orderedOrders
                .Skip(
                    (request.Page - 1)
                    * request.PageSize)
                .Take(request.PageSize)
                .ToList();


        // ================================================
        // BUILD RESPONSE
        // ================================================

        var result =
            new List<OrderResponseDTO>();

        foreach (var order in pagedOrders)
        {
            result.Add(
                await BuildResponse(order));
        }


        return new PagedResponseDTO<OrderResponseDTO>
        {
            Items =
                result,

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
    // CANCEL BY CUSTOMER
    // =====================================================

    public async Task<OrderResponseDTO>
        CancelAsync(
            long orderId,
            long customerId)
    {
        return await _unitOfWork
            .ExecuteInTransactionAsync(
                async () =>
                {
                    var order =
                        await _unitOfWork.Orders
                            .GetByIdAsync(
                                orderId);

                    if (order == null)
                    {
                        throw new NotFoundException(
                            "Order không tồn tại.");
                    }


                    if (order.CustomerId !=
                        customerId)
                    {
                        throw new ForbiddenException(
                            "Bạn không có quyền hủy Order này.");
                    }


                    if (order.Status is not (
                        OrderStatuses.Pending
                        or "awaiting_payment"))
                    {
                        throw new BadRequestException(
                            "Chỉ có thể hủy đơn đang chờ xác nhận hoặc chờ thanh toán.");
                    }


                    var items =
                        await _unitOfWork.OrderItems
                            .FindAsync(
                                x =>
                                    x.OrderId ==
                                    order.Id);


                    foreach (var item in items)
                    {
                        await ReleaseInventory(
                            item.VariantId,
                            item.Quantity);
                    }


                    // ========================================
                    // RELEASE VOUCHER
                    // ========================================

                    await ReleaseVoucher(
                        order.Id);


                    // ========================================
                    // CANCEL PAYMENT
                    // ========================================

                    var payments =
                        await _unitOfWork
                            .PaymentTransactions
                            .FindAsync(
                                x =>
                                    x.OrderId ==
                                    order.Id);

                    foreach (var payment in payments)
                    {
                        if (payment.Status ==
                            "pending")
                        {
                            payment.Status =
                                "cancelled";

                            payment.PaidAt =
                                null;

                            _unitOfWork
                                .PaymentTransactions
                                .Update(payment);
                        }
                    }


                    string oldStatus =
                        order.Status;

                    order.Status =
                        OrderStatuses.Cancelled;

                    order.CancelReason =
                        "Khách hàng hủy đơn.";

                    order.CancelledAt =
                        DateTime.UtcNow;

                    order.UpdatedAt =
                        DateTime.UtcNow;

                    _unitOfWork.Orders
                        .Update(order);


                    await _unitOfWork.OrderStatusLogs
                        .AddAsync(
                            new OrderStatusLogModel
                            {
                                OrderId =
                                    order.Id,

                                FromStatus =
                                    oldStatus,

                                ToStatus =
                                    OrderStatuses.Cancelled,

                                Message =
                                    "Customer hủy Order.",

                                CreatedByUserId =
                                    customerId,

                                CreatedAt =
                                    DateTime.UtcNow
                            });


                    await _unitOfWork.SaveChangesAsync();

                    return await BuildResponse(
                        order);
                });
    }


    // =====================================================
    // UPDATE STATUS
    // SELLER / ADMIN
    // =====================================================

    public async Task<OrderResponseDTO>
        UpdateStatusAsync(
            long orderId,
            long actorUserId,
            string role,
            UpdateOrderStatusDTO request)
    {
        return await _unitOfWork
            .ExecuteInTransactionAsync(
                async () =>
                {
                    if (request == null)
                    {
                        throw new BadRequestException(
                            "Request không được null.");
                    }


                    var order =
                        await _unitOfWork.Orders
                            .GetByIdAsync(
                                orderId);

                    if (order == null)
                    {
                        throw new NotFoundException(
                            "Order không tồn tại.");
                    }


                    await CheckOrderAccess(
                        order,
                        actorUserId,
                        role);


                    if (string.IsNullOrWhiteSpace(
                        request.Status))
                    {
                        throw new BadRequestException(
                            "Status không được để trống.");
                    }


                    string newStatus =
                        request.Status
                            .Trim()
                            .ToLowerInvariant();


                    ValidateStatusTransition(
                        order.Status,
                        newStatus,
                        role);


                    string oldStatus =
                        order.Status;


                    // ==================================
                    // CANCEL
                    // ==================================

                    if (newStatus ==
                        OrderStatuses.Cancelled)
                    {
                        var items =
                            await _unitOfWork
                                .OrderItems
                                .FindAsync(
                                    x =>
                                        x.OrderId ==
                                        order.Id);


                        foreach (var item in items)
                        {
                            await ReleaseInventory(
                                item.VariantId,
                                item.Quantity);
                        }


                        // ========================================
                        // RELEASE VOUCHER
                        // ========================================

                        await ReleaseVoucher(
                            order.Id);


                        // ========================================
                        // CANCEL PAYMENT
                        // ========================================

                        var payments =
                            await _unitOfWork
                                .PaymentTransactions
                                .FindAsync(
                                    x =>
                                        x.OrderId ==
                                        order.Id);

                        foreach (var payment in payments)
                        {
                            if (payment.Status ==
                                "pending")
                            {
                                payment.Status =
                                    "cancelled";

                                payment.PaidAt =
                                    null;

                                _unitOfWork
                                    .PaymentTransactions
                                    .Update(payment);
                            }
                        }
                    }


                    order.Status =
                        newStatus;

                    if (newStatus == OrderStatuses.Confirmed)
                    {
                        order.ConfirmedAt = DateTime.UtcNow;
                        order.ConfirmedByUserId = actorUserId;
                    }

                    order.UpdatedAt =
                        DateTime.UtcNow;

                    _unitOfWork.Orders
                        .Update(order);


                    await _unitOfWork.OrderStatusLogs
                        .AddAsync(
                            new OrderStatusLogModel
                            {
                                OrderId =
                                    order.Id,

                                FromStatus =
                                    oldStatus,

                                ToStatus =
                                    newStatus,

                                Message =
                                    request.Message,

                                CreatedByUserId =
                                    actorUserId,

                                CreatedAt =
                                    DateTime.UtcNow
                            });


                    await _unitOfWork.SaveChangesAsync();

                    return await BuildResponse(
                        order);
                });
    }


    // =====================================================
    // SHOP CONFIRM
    // =====================================================

    public async Task<OrderResponseDTO> ConfirmByShopAsync(
        long orderId,
        long actorUserId,
        string role)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrder(orderId);
            await EnsureShopActor(order, actorUserId, role);

            var current = Normalize(order.Status);
            if (current is not (OrderStatuses.Pending or "paid"))
            {
                throw new BadRequestException(
                    "Chỉ xác nhận được đơn đang chờ shop xác nhận.");
            }

            order.Status = OrderStatuses.Confirmed;
            order.ConfirmedAt = DateTime.UtcNow;
            order.ConfirmedByUserId = actorUserId;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);

            await AddStatusLog(
                order.Id,
                current,
                OrderStatuses.Confirmed,
                "Shop đã xác nhận đơn.",
                actorUserId);

            await EnsureShipmentAsync(order);
            await _unitOfWork.SaveChangesAsync();
            return await BuildResponse(order);
        });
    }


    // =====================================================
    // SHOP CANCEL
    // =====================================================

    public async Task<OrderResponseDTO> CancelByShopAsync(
        long orderId,
        long actorUserId,
        string role,
        CancelOrderDTO request)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new BadRequestException(
                    "Vui lòng nhập lý do hủy đơn.");
            }

            var reason = request.Reason.Trim();
            if (reason.Length > 500)
            {
                throw new BadRequestException(
                    "Lý do hủy không được dài quá 500 ký tự.");
            }

            var order = await GetRequiredOrder(orderId);
            await EnsureShopActor(order, actorUserId, role);

            var current = Normalize(order.Status);
            if (current is not (OrderStatuses.Pending or "paid"))
            {
                throw new BadRequestException(
                    "Chỉ hủy được đơn đang chờ shop xác nhận.");
            }

            await ReleaseOrderResources(order.Id);

            order.Status = OrderStatuses.Cancelled;
            order.CancelReason = reason;
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);

            await AddStatusLog(
                order.Id,
                current,
                OrderStatuses.Cancelled,
                reason,
                actorUserId);

            await _unitOfWork.SaveChangesAsync();
            return await BuildResponse(order);
        });
    }


    // =====================================================
    // SHIPPER LISTS
    // =====================================================

    public async Task<List<OrderResponseDTO>> GetAvailableForShipperAsync(
        long shipperUserId)
    {
        await EnsureShipperAsync(shipperUserId);

        var assigned = await _unitOfWork.Shipments
            .FindAsync(shipment => shipment.ShipperUserId != null);
        var takenIds = assigned
            .Select(shipment => shipment.OrderId)
            .ToHashSet();

        var orders = await _unitOfWork.Orders.FindAsync(order =>
            order.Status == OrderStatuses.Confirmed ||
            order.Status == OrderStatuses.Processing);

        var result = new List<OrderResponseDTO>();
        foreach (var order in orders
            .Where(order => !takenIds.Contains(order.Id))
            .OrderBy(order => order.PlacedAt)
            .Take(100))
        {
            result.Add(await BuildResponse(order));
        }

        return result;
    }

    public async Task<List<OrderResponseDTO>> GetShipperDeliveriesAsync(
        long shipperUserId)
    {
        await EnsureShipperAsync(shipperUserId);

        var mine = await _unitOfWork.Shipments
            .FindAsync(shipment => shipment.ShipperUserId == shipperUserId);
        var orderIds = mine
            .Select(shipment => shipment.OrderId)
            .ToHashSet();

        var orders = await _unitOfWork.Orders
            .FindAsync(order => orderIds.Contains(order.Id));

        var result = new List<OrderResponseDTO>();
        foreach (var order in orders.OrderByDescending(order => order.UpdatedAt ?? order.PlacedAt))
        {
            result.Add(await BuildResponse(order));
        }

        return result;
    }


    // =====================================================
    // SHIPPER ACCEPT
    // =====================================================

    public async Task<OrderResponseDTO> AcceptByShipperAsync(
        long orderId,
        long shipperUserId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await EnsureShipperAsync(shipperUserId);
            var order = await GetRequiredOrder(orderId);
            var current = Normalize(order.Status);

            if (current == OrderStatuses.Shipping)
            {
                return await AcceptOwnedOrReject(order, shipperUserId);
            }

            if (current is not (OrderStatuses.Confirmed or OrderStatuses.Processing))
            {
                throw new BadRequestException(
                    "Chỉ nhận được đơn shop đã xác nhận.");
            }

            await EnsureShipmentRowAsync(order);

            var now = DateTime.UtcNow;
            var claimed = await _unitOfWork.ClaimOpenShipmentAsync(
                order.Id,
                shipperUserId,
                now);

            if (claimed == 0)
            {
                return await AcceptOwnedOrReject(order, shipperUserId);
            }

            var moved = await _unitOfWork.MarkOrderShippingIfOpenAsync(
                order.Id,
                now);

            if (moved == 0)
            {
                throw new BadRequestException(
                    "Đơn hàng đã được shipper khác nhận.");
            }

            order.Status = OrderStatuses.Shipping;
            order.UpdatedAt = now;

            var shipments = await _unitOfWork.Shipments
                .FindAsync(item => item.OrderId == order.Id);
            var shipment = shipments.FirstOrDefault()
                ?? throw new BadRequestException(
                    "Đơn hàng đã được shipper khác nhận.");

            await _unitOfWork.ShipmentEvents.AddAsync(new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                Status = "shipping",
                Note = "Shipper đã nhận hàng và bắt đầu giao.",
                CreatedAt = now
            });

            await AddStatusLog(
                order.Id,
                current,
                OrderStatuses.Shipping,
                "Shipper đã nhận hàng.",
                shipperUserId);

            await _unitOfWork.SaveChangesAsync();
            return await BuildResponse(order);
        });
    }


    private async Task<OrderResponseDTO> AcceptOwnedOrReject(
        OrderModel order,
        long shipperUserId)
    {
        var shipments = await _unitOfWork.Shipments
            .FindAsync(item => item.OrderId == order.Id);
        var shipment = shipments.FirstOrDefault();

        if (shipment?.ShipperUserId == shipperUserId)
        {
            if (Normalize(order.Status) == OrderStatuses.Shipping)
            {
                return await BuildResponse(order);
            }

            throw new BadRequestException(
                "Bạn đã nhận đơn này.");
        }

        throw new BadRequestException(
            "Đơn hàng đã được shipper khác nhận.");
    }


    private async Task EnsureShipmentRowAsync(OrderModel order)
    {
        var shipment = await EnsureShipmentAsync(order);
        if (shipment.Id > 0)
        {
            return;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new BadRequestException(
                "Đơn hàng đã được shipper khác nhận.");
        }
    }


    // =====================================================
    // SHIPPER DELIVERED TO CUSTOMER
    // =====================================================

    public async Task<OrderResponseDTO> DeliverByShipperAsync(
        long orderId,
        long shipperUserId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await EnsureShipperAsync(shipperUserId);
            var order = await GetRequiredOrder(orderId);
            var current = Normalize(order.Status);

            if (current != OrderStatuses.Shipping)
            {
                throw new BadRequestException(
                    "Chỉ xác nhận giao khi đơn đang giao và bạn đã nhận đơn.");
            }

            var shipments = await _unitOfWork.Shipments
                .FindAsync(item => item.OrderId == order.Id);
            var shipment = shipments.FirstOrDefault()
                ?? throw new BadRequestException(
                    "Đơn chưa được nhận giao.");

            if (shipment.ShipperUserId != shipperUserId)
            {
                throw new ForbiddenException(
                    "Bạn không phải người giao của đơn này.");
            }

            var now = DateTime.UtcNow;
            shipment.Status = "delivered";
            shipment.DeliveredAt = now;
            shipment.UpdatedAt = now;
            _unitOfWork.Shipments.Update(shipment);

            await _unitOfWork.ShipmentEvents.AddAsync(new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                Status = "delivered",
                Note = "Shipper đã giao, chờ khách xác nhận.",
                CreatedAt = now
            });

            await MarkCodPaidAsync(order);

            order.Status = OrderStatuses.AwaitingReceipt;
            order.UpdatedAt = now;
            _unitOfWork.Orders.Update(order);

            await AddStatusLog(
                order.Id,
                current,
                OrderStatuses.AwaitingReceipt,
                "Shipper đã giao. Chờ khách xác nhận đã nhận hàng.",
                shipperUserId);

            await _unitOfWork.SaveChangesAsync();
            return await BuildResponse(order);
        });
    }


    // =====================================================
    // CUSTOMER CONFIRMS RECEIPT
    // =====================================================

    public async Task<OrderResponseDTO> ConfirmReceivedAsync(
        long orderId,
        long customerId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrder(orderId);
            if (order.CustomerId != customerId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xác nhận đơn này.");
            }

            var current = Normalize(order.Status);
            if (current == OrderStatuses.Delivered)
            {
                throw new BadRequestException(
                    "Đơn đã được xác nhận nhận hàng.");
            }

            if (current != OrderStatuses.AwaitingReceipt)
            {
                throw new BadRequestException(
                    "Chỉ xác nhận nhận hàng sau khi shipper đã giao.");
            }

            var now = DateTime.UtcNow;
            order.Status = OrderStatuses.Delivered;
            order.CustomerConfirmedAt = now;
            order.UpdatedAt = now;
            _unitOfWork.Orders.Update(order);

            await SettleDeliveredOrderAsync(order);

            await AddStatusLog(
                order.Id,
                current,
                OrderStatuses.Delivered,
                "Khách đã xác nhận nhận hàng.",
                customerId);

            await _unitOfWork.SaveChangesAsync();
            return await BuildResponse(order);
        });
    }


    // =====================================================
    // RESERVE INVENTORY
    // =====================================================

    private async Task CancelUnpaidCheckoutsAsync(long customerId)
    {
        var unpaid = await _unitOfWork.Orders.FindAsync(order =>
            order.CustomerId == customerId &&
            order.Status == "awaiting_payment");

        foreach (var order in unpaid)
        {
            var items = await _unitOfWork.OrderItems
                .FindAsync(item => item.OrderId == order.Id);

            foreach (var item in items)
            {
                await ReleaseInventory(item.VariantId, item.Quantity);
            }

            var payments = await _unitOfWork.PaymentTransactions
                .FindAsync(payment => payment.OrderId == order.Id);

            foreach (var payment in payments)
            {
                if (payment.Status == "pending")
                {
                    payment.Status = "cancelled";
                    payment.PaidAt = null;
                    _unitOfWork.PaymentTransactions.Update(payment);
                }
            }

            var previous = order.Status;
            order.Status = OrderStatuses.Cancelled;
            order.CancelReason = "Khách chưa thanh toán.";
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);

            await AddStatusLog(
                order.Id,
                previous,
                OrderStatuses.Cancelled,
                "Đơn QR chưa thanh toán được hủy khi khách tạo lại đơn. Giỏ hàng được giữ.",
                customerId);
        }
    }


    private async Task RemoveOrderedCartItems(
        long customerId,
        List<OrderItemModel> orderItems)
    {
        var carts = await _unitOfWork.Carts
            .FindAsync(cart => cart.UserId == customerId);

        var cart = carts.FirstOrDefault();
        if (cart == null)
        {
            return;
        }

        var variantIds = orderItems
            .Select(item => item.VariantId)
            .ToHashSet();

        var lines = await _unitOfWork.CartItems
            .FindAsync(item =>
                item.CartId == cart.Id &&
                variantIds.Contains(item.VariantId));

        foreach (var line in lines)
        {
            _unitOfWork.CartItems.Delete(line);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Carts.Update(cart);
    }


    private async Task ReserveInventory(
        long variantId,
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new BadRequestException(
                "Quantity Reserve phải lớn hơn 0.");
        }


        var inventory =
            await _unitOfWork.Inventories
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.ProductVariantId == variantId);

        if (inventory == null)
        {
            throw new NotFoundException(
                $"Inventory của Variant {variantId} không tồn tại.");
        }


        int available =
            inventory.Quantity
            - inventory.ReservedQuantity;


        if (quantity > available)
        {
            throw new BadRequestException(
                $"Variant {variantId} không đủ tồn kho.");
        }


        inventory.ReservedQuantity +=
            quantity;

        inventory.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Inventories
            .Update(inventory);

        await _unitOfWork.SaveChangesAsync();
    }


    // =====================================================
    // RELEASE INVENTORY
    // =====================================================

    private async Task ReleaseInventory(
        long variantId,
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new BadRequestException(
                "Quantity Release phải lớn hơn 0.");
        }


        var inventory =
            await _unitOfWork.Inventories
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.ProductVariantId == variantId);

        if (inventory == null)
        {
            throw new NotFoundException(
                $"Inventory của Variant {variantId} không tồn tại.");
        }


        if (quantity >
            inventory.ReservedQuantity)
        {
            throw new BadRequestException(
                $"ReservedQuantity của Variant {variantId} không đủ.");
        }


        inventory.ReservedQuantity -=
            quantity;

        inventory.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Inventories
            .Update(inventory);

        await _unitOfWork.SaveChangesAsync();
    }


    // =====================================================
    // RELEASE VOUCHER
    // =====================================================

    private async Task ReleaseVoucher(
        long orderId)
    {
        var orderVouchers =
            await _unitOfWork.OrderVouchers
                .FindAsync(
                    x =>
                        x.OrderId ==
                        orderId);


        foreach (var orderVoucher in orderVouchers)
        {
            var voucher =
                await _unitOfWork.Vouchers
                    .GetByIdAsync(
                        orderVoucher.VoucherId);


            if (voucher != null &&
                voucher.UsedCount > 0)
            {
                voucher.UsedCount--;

                _unitOfWork.Vouchers
                    .Update(voucher);
            }


            _unitOfWork.OrderVouchers
                .Delete(orderVoucher);
        }
    }


    // =====================================================
    // CHECK ACCESS
    // =====================================================

    private async Task CheckOrderAccess(
        OrderModel order,
        long userId,
        string role)
    {
        role =
            role.Trim()
                .ToLowerInvariant();

        if (order.CustomerId == userId)
        {
            return;
        }


        // ================================================
        // ADMIN
        // ================================================

        if (role == "admin")
        {
            return;
        }


        // ================================================
        // CUSTOMER
        // ================================================

        if (role == "customer")
        {
            if (order.CustomerId !=
                userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền truy cập Order này.");
            }

            return;
        }


        // ================================================
        // SELLER
        // ================================================

        if (role == "seller")
        {
            bool ownsShop =
                await _unitOfWork.Shops
                    .AnyAsync(
                        x =>
                            x.Id ==
                                order.ShopId
                            &&
                            x.OwnerUserId ==
                                userId);


            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền truy cập Order của Shop này.");
            }

            return;
        }


        if (role == "shipper")
        {
            var shipments =
                await _unitOfWork.Shipments
                    .FindAsync(item => item.OrderId == order.Id);

            var shipment = shipments.FirstOrDefault();
            if (shipment?.ShipperUserId == userId)
            {
                return;
            }

            var status = Normalize(order.Status);
            if (shipment?.ShipperUserId == null &&
                status is OrderStatuses.Confirmed or OrderStatuses.Processing)
            {
                return;
            }

            throw new ForbiddenException(
                "Bạn không có quyền xem đơn này.");
        }


        throw new ForbiddenException(
            "Role không được phép truy cập Order.");
    }


    // =====================================================
    // STATUS TRANSITION
    // =====================================================

    private void ValidateStatusTransition(
        string currentStatus,
        string newStatus,
        string role)
    {
        currentStatus =
            currentStatus
                .Trim()
                .ToLowerInvariant();

        newStatus =
            newStatus
                .Trim()
                .ToLowerInvariant();

        role =
            role
                .Trim()
                .ToLowerInvariant();


        // ================================================
        // FINAL STATUS
        // ================================================

        if (currentStatus ==
            OrderStatuses.Delivered)
        {
            throw new BadRequestException(
                "Order đã delivered và không thể thay đổi.");
        }


        if (currentStatus ==
            OrderStatuses.Cancelled)
        {
            throw new BadRequestException(
                "Order đã cancelled và không thể thay đổi.");
        }


        // ================================================
        // SELLER
        // ================================================

        if (role == "seller")
        {
            bool valid =
                (
                    currentStatus == OrderStatuses.Pending
                    ||
                    currentStatus == "paid"
                )
                &&
                newStatus == OrderStatuses.Confirmed;

            if (!valid)
            {
                throw new BadRequestException(
                    "Seller chỉ được xác nhận đơn đang chờ shop. Hủy đơn phải kèm lý do.");
            }

            return;
        }


        // ================================================
        // ADMIN
        // ================================================

        if (role == "admin")
        {
            bool valid =
                (
                    currentStatus == OrderStatuses.Pending
                    ||
                    currentStatus == "paid"
                )
                &&
                newStatus == OrderStatuses.Confirmed;

            if (!valid)
            {
                throw new BadRequestException(
                    "Admin chỉ được xác nhận đơn đang chờ shop. Không chuyển trạng thái giao hàng tại đây.");
            }

            return;
        }


        throw new ForbiddenException(
            "Role không được phép thay đổi trạng thái Order.");
    }


    // =====================================================
    // BUILD RESPONSE
    // =====================================================

    private async Task<OrderResponseDTO>
        BuildResponse(
            OrderModel order)
    {
        var result =
            new OrderResponseDTO
            {
                Id =
                    order.Id,

                OrderCode =
                    order.OrderCode,

                CustomerId =
                    order.CustomerId,

                ShopId =
                    order.ShopId,

                ShippingAddressId =
                    order.ShippingAddressId,

                Status =
                    order.Status,

                Currency =
                    order.Currency,

                Subtotal =
                    order.Subtotal,

                ShippingFee =
                    order.ShippingFee,

                DiscountTotal =
                    order.DiscountTotal,

                Total =
                    order.Total,

                PaymentMethod =
                    order.PaymentMethod,

                Note =
                    order.Note,

                CancelReason =
                    order.CancelReason,

                ConfirmedAt =
                    order.ConfirmedAt,

                ConfirmedByUserId =
                    order.ConfirmedByUserId,

                CancelledAt =
                    order.CancelledAt,

                CustomerConfirmedAt =
                    order.CustomerConfirmedAt,

                PlacedAt =
                    order.PlacedAt,

                UpdatedAt =
                    order.UpdatedAt
            };


        // ================================================
        // ITEMS
        // ================================================

        var items =
            await _unitOfWork.OrderItems
                .FindAsync(
                    x =>
                        x.OrderId ==
                        order.Id);


        result.Items =
            items
                .Select(
                    x =>
                        new OrderItemResponseDTO
                        {
                            Id =
                                x.Id,

                            ProductId =
                                x.ProductId,

                            VariantId =
                                x.VariantId,

                            ProductName =
                                x.ProductNameSnapshot,

                            VariantName =
                                x.VariantNameSnapshot,

                            UnitPrice =
                                x.UnitPrice,

                            Quantity =
                                x.Quantity,

                            LineTotal =
                                x.LineTotal
                        })
                .ToList();

        var productIds = result.Items
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();
        if (productIds.Count > 0)
        {
            var images = await _unitOfWork.ProductImages
                .FindAsync(image => productIds.Contains(image.ProductId));
            var firstByProduct = images
                .Where(image => !string.IsNullOrWhiteSpace(image.Url))
                .GroupBy(image => image.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(image => image.SortOrder).First().Url);

            foreach (var item in result.Items)
            {
                if (firstByProduct.TryGetValue(item.ProductId, out var url))
                {
                    item.ImageUrl = url;
                }
            }
        }


        // ================================================
        // STATUS LOGS
        // ================================================

        var logs =
            await _unitOfWork.OrderStatusLogs
                .FindAsync(
                    x =>
                        x.OrderId ==
                        order.Id);


        result.StatusLogs =
            logs
                .Select(
                    x =>
                        new OrderStatusLogResponseDTO
                        {
                            Id =
                                x.Id,

                            FromStatus =
                                x.FromStatus,

                            ToStatus =
                                x.ToStatus,

                            Message =
                                x.Message,

                            CreatedByUserId =
                                x.CreatedByUserId,

                            CreatedAt =
                                x.CreatedAt
                        })
                .OrderBy(
                    x => x.CreatedAt)
                .ToList();


        var customer =
            await _unitOfWork.Users.GetByIdAsync(order.CustomerId);
        var shop =
            await _unitOfWork.Shops.GetByIdAsync(order.ShopId);
        var address =
            await _unitOfWork.Addresses.GetByIdAsync(
                order.ShippingAddressId);
        var orderShipments =
            await _unitOfWork.Shipments.FindAsync(
                item => item.OrderId == order.Id);
        var orderShipment = orderShipments.FirstOrDefault();

        result.CustomerName =
            customer?.FullName ?? address?.ReceiverName;
        result.CustomerPhone =
            address?.ReceiverPhone ?? customer?.Phone;
        result.ShopName = shop?.Name;
        result.ReceiverName = address?.ReceiverName;
        result.ReceiverPhone = address?.ReceiverPhone;
        result.ShippingAddressText = JoinAddress(address);

        if (orderShipment != null)
        {
            result.ShipperId = orderShipment.ShipperUserId;
            result.ShipperAcceptedAt =
                orderShipment.AssignedAt ?? orderShipment.PickedAt;
            result.DeliveredAt = orderShipment.DeliveredAt;

            if (orderShipment.ShipperUserId != null)
            {
                var shipperUser =
                    await _unitOfWork.Users.GetByIdAsync(
                        orderShipment.ShipperUserId.Value);
                result.ShipperName = shipperUser?.FullName;
            }
        }


        return result;
    }


    // =====================================================
    // VALIDATE PAGINATION
    // =====================================================

    private void ValidatePagination(
        OrderPaginationRequestDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }
    }


    // =====================================================
    // ORDER CODE
    // =====================================================

    private string GenerateOrderCode()
    {
        return
            $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"
                .Substring(
                    0,
                    40);
    }


    private static bool MatchesCustomerStatusFilter(
        string? orderStatus,
        string filter)
    {
        var status = (orderStatus ?? "").Trim().ToLowerInvariant();
        return filter switch
        {
            "pending" => status is "pending" or "paid",
            "confirmed" => status is "confirmed" or "processing",
            "shipping" => status is "shipping" or "shipped",
            "delivered" => status is "delivered" or "completed",
            _ => status == filter
        };
    }

    private static string Normalize(string? status) =>
        status?.Trim().ToLowerInvariant() ?? "";


    private async Task<OrderModel> GetRequiredOrder(long orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new NotFoundException("Order không tồn tại.");
        }

        return order;
    }


    private async Task EnsureShopActor(
        OrderModel order,
        long actorUserId,
        string role)
    {
        role = Normalize(role);
        if (role == "admin")
        {
            return;
        }

        if (role != "seller")
        {
            throw new ForbiddenException(
                "Bạn không có quyền xác nhận hoặc hủy đơn của shop.");
        }

        bool ownsShop = await _unitOfWork.Shops.AnyAsync(shop =>
            shop.Id == order.ShopId &&
            shop.OwnerUserId == actorUserId);

        if (!ownsShop)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xử lý đơn của shop này.");
        }
    }


    private async Task EnsureShipperAsync(long userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new ForbiddenException(
                "Không xác định được tài khoản shipper.");

        if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(
                "Tài khoản shipper không hoạt động.");
        }

        var userRoles = await _unitOfWork.UserRoles
            .FindAsync(item => item.UserId == userId);

        foreach (var userRole in userRoles)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(userRole.RoleId);
            if (role != null &&
                string.Equals(role.Name, "shipper", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        throw new ForbiddenException(
            "Chỉ shipper mới được nhận và giao đơn.");
    }


    private async Task AddStatusLog(
        long orderId,
        string fromStatus,
        string toStatus,
        string message,
        long userId)
    {
        await _unitOfWork.OrderStatusLogs.AddAsync(new OrderStatusLogModel
        {
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Message = message,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        });
    }


    private async Task ReleaseOrderResources(long orderId)
    {
        var items = await _unitOfWork.OrderItems
            .FindAsync(item => item.OrderId == orderId);

        foreach (var item in items)
        {
            await ReleaseInventory(item.VariantId, item.Quantity);
        }

        await ReleaseVoucher(orderId);

        var payments = await _unitOfWork.PaymentTransactions
            .FindAsync(payment => payment.OrderId == orderId);

        foreach (var payment in payments)
        {
            if (payment.Status == "pending")
            {
                payment.Status = "cancelled";
                payment.PaidAt = null;
                _unitOfWork.PaymentTransactions.Update(payment);
            }
        }
    }


    private async Task<ShipmentModel> EnsureShipmentAsync(OrderModel order)
    {
        var existing = await _unitOfWork.Shipments
            .FindAsync(item => item.OrderId == order.Id);
        var shipment = existing.FirstOrDefault();
        if (shipment != null)
        {
            return shipment;
        }

        var now = DateTime.UtcNow;
        shipment = new ShipmentModel
        {
            OrderId = order.Id,
            TrackingCode = $"ML{order.Id:D6}{now:HHmmss}",
            Status = "created",
            CodAmount = string.Equals(order.PaymentMethod, "cod", StringComparison.OrdinalIgnoreCase)
                ? order.Total
                : 0,
            CreatedAt = now
        };

        await _unitOfWork.Shipments.AddAsync(shipment);
        return shipment;
    }


    private async Task MarkCodPaidAsync(OrderModel order)
    {
        if (!string.Equals(order.PaymentMethod, "cod", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var payments = await _unitOfWork.PaymentTransactions
            .FindAsync(payment => payment.OrderId == order.Id);
        var payment = payments
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();
        var now = DateTime.UtcNow;

        if (payment == null)
        {
            await _unitOfWork.PaymentTransactions.AddAsync(new PaymentTransaction
            {
                OrderId = order.Id,
                Method = "cod",
                Amount = order.Total,
                Status = "paid",
                PaidAt = now,
                CreatedAt = now
            });
            return;
        }

        if (string.Equals(payment.Status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = "paid";
            payment.PaidAt = now;
            _unitOfWork.PaymentTransactions.Update(payment);
        }
    }


    private async Task SettleDeliveredOrderAsync(OrderModel order)
    {
        var items = await _unitOfWork.OrderItems
            .FindAsync(item => item.OrderId == order.Id);

        foreach (var item in items)
        {
            var inventories = await _unitOfWork.Inventories
                .FindAsync(row => row.ProductVariantId == item.VariantId);
            var inventory = inventories.FirstOrDefault()
                ?? throw new NotFoundException(
                    $"Inventory của Variant {item.VariantId} không tồn tại.");

            if (item.Quantity > inventory.ReservedQuantity ||
                item.Quantity > inventory.Quantity)
            {
                throw new BadRequestException(
                    $"Tồn kho của Variant {item.VariantId} không đủ để hoàn tất giao hàng.");
            }

            inventory.ReservedQuantity -= item.Quantity;
            inventory.Quantity -= item.Quantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Inventories.Update(inventory);
        }

        var credits = await _unitOfWork.ShopWalletTransactions
            .FindAsync(row =>
                row.OrderId == order.Id &&
                row.Type == "SALE_CREDIT");

        if (credits.Any())
        {
            return;
        }

        var wallets = await _unitOfWork.ShopWallets
            .FindAsync(row => row.ShopId == order.ShopId);
        var wallet = wallets.FirstOrDefault();
        if (wallet == null)
        {
            wallet = new MiniLogistics.DAL.Models.ShopWallet
            {
                ShopId = order.ShopId,
                Balance = 0,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.ShopWallets.AddAsync(wallet);
            await _unitOfWork.SaveChangesAsync();
        }

        wallet.Balance += order.Total;
        wallet.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.ShopWallets.Update(wallet);

        await _unitOfWork.ShopWalletTransactions.AddAsync(
            new MiniLogistics.DAL.Models.ShopWalletTransaction
            {
                WalletId = wallet.Id,
                OrderId = order.Id,
                Type = "SALE_CREDIT",
                Amount = order.Total,
                Description = $"Doanh thu đơn {order.OrderCode}",
                CreatedAt = DateTime.UtcNow
            });
    }


    private static string? JoinAddress(AddressModel? address)
    {
        if (address == null)
        {
            return null;
        }

        var parts = new[]
        {
            address.Line1,
            address.Line2,
            address.Ward,
            address.District,
            address.Province
        };

        var text = string.Join(
            ", ",
            parts.Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}