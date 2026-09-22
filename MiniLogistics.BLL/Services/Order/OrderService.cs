using MiniLogistics.BLL.DTOs.Order;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

using OrderModel = MiniLogistics.DAL.Models.Order;
using OrderItemModel = MiniLogistics.DAL.Models.OrderItem;
using OrderStatusLogModel = MiniLogistics.DAL.Models.OrderStatusLog;

namespace MiniLogistics.BLL.Services.Order;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    private const decimal ShippingFee = 0m;

    public OrderService(IUnitOfWork unitOfWork)
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
            request.PaymentMethod.Trim().ToLowerInvariant();

        if (paymentMethod != "cod")
        {
            throw new BadRequestException(
                "Hiện tại Order chỉ hỗ trợ phương thức COD.");
        }

        // ================================================
        // CHECK CUSTOMER
        // ================================================

        var customer =
            await _unitOfWork.Users.GetByIdAsync(customerId);

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
                        variant.Price
                        * requestItem.Quantity;

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
                        .GetByIdAsync(shopId.Value);

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
                            OrderStatuses.Pending,

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
                            string.IsNullOrWhiteSpace(request.Note)
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
                            OrderStatuses.Pending,

                        Message =
                            "Order được tạo.",

                        CreatedByUserId =
                            customerId,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                await _unitOfWork.OrderStatusLogs
                    .AddAsync(statusLog);

                await _unitOfWork.SaveChangesAsync();

                // ========================================
                // RESPONSE
                // ========================================

                return await BuildResponse(order);
            });
    }

    // =====================================================
    // GET MY ORDERS
    // =====================================================

    public async Task<IEnumerable<OrderResponseDTO>>
        GetMyOrdersAsync(long customerId)
    {
        var orders =
            await _unitOfWork.Orders
                .FindAsync(
                    x =>
                        x.CustomerId
                        == customerId);

        var result =
            new List<OrderResponseDTO>();

        foreach (var order in orders)
        {
            result.Add(
                await BuildResponse(order));
        }

        return result;
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
                .GetByIdAsync(orderId);

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
    // =====================================================

    public async Task<IEnumerable<OrderResponseDTO>>
        GetAllAsync()
    {
        var orders =
            await _unitOfWork.Orders
                .GetAllAsync();

        var result =
            new List<OrderResponseDTO>();

        foreach (var order in orders)
        {
            result.Add(
                await BuildResponse(order));
        }

        return result;
    }

    // =====================================================
    // GET BY SHOP OWNER
    // =====================================================

    public async Task<IEnumerable<OrderResponseDTO>>
        GetByShopOwnerAsync(
            long sellerUserId)
    {
        var shops =
            await _unitOfWork.Shops
                .FindAsync(
                    x =>
                        x.OwnerUserId
                        == sellerUserId);

        var shopIds =
            shops
                .Select(x => x.Id)
                .ToHashSet();

        var orders =
            await _unitOfWork.Orders
                .GetAllAsync();

        var result =
            new List<OrderResponseDTO>();

        foreach (var order in orders)
        {
            if (!shopIds.Contains(order.ShopId))
            {
                continue;
            }

            result.Add(
                await BuildResponse(order));
        }

        return result;
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
                            .GetByIdAsync(orderId);

                    if (order == null)
                    {
                        throw new NotFoundException(
                            "Order không tồn tại.");
                    }

                    if (order.CustomerId != customerId)
                    {
                        throw new ForbiddenException(
                            "Bạn không có quyền hủy Order này.");
                    }

                    if (order.Status !=
                        OrderStatuses.Pending)
                    {
                        throw new BadRequestException(
                            "Chỉ có thể hủy Order đang ở trạng thái pending.");
                    }

                    var items =
                        await _unitOfWork.OrderItems
                            .FindAsync(
                                x =>
                                    x.OrderId
                                    == order.Id);

                    foreach (var item in items)
                    {
                        await ReleaseInventory(
                            item.VariantId,
                            item.Quantity);
                    }

                    string oldStatus =
                        order.Status;

                    order.Status =
                        OrderStatuses.Cancelled;

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

                    return await BuildResponse(order);
                });
    }

    // =====================================================
    // UPDATE STATUS
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
                            .GetByIdAsync(orderId);

                    if (order == null)
                    {
                        throw new NotFoundException(
                            "Order không tồn tại.");
                    }

                    await CheckOrderAccess(
                        order,
                        actorUserId,
                        role);

                    if (string.IsNullOrWhiteSpace(request.Status))
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
                            await _unitOfWork.OrderItems
                                .FindAsync(
                                    x =>
                                        x.OrderId
                                        == order.Id);

                        foreach (var item in items)
                        {
                            await ReleaseInventory(
                                item.VariantId,
                                item.Quantity);
                        }
                    }

                    order.Status =
                        newStatus;

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

                    return await BuildResponse(order);
                });
    }

    // =====================================================
    // RESERVE INVENTORY
    // =====================================================

    private async Task ReserveInventory(
        long variantId,
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new BadRequestException(
                "Quantity Reserve phải lớn hơn 0.");
        }

        var inventories =
            await _unitOfWork.Inventories
                .FindAsync(
                    x =>
                        x.ProductVariantId
                        == variantId);

        var inventory =
            inventories.FirstOrDefault();

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

        var inventories =
            await _unitOfWork.Inventories
                .FindAsync(
                    x =>
                        x.ProductVariantId
                        == variantId);

        var inventory =
            inventories.FirstOrDefault();

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
    // CHECK ACCESS
    // =====================================================

    private async Task CheckOrderAccess(
        OrderModel order,
        long userId,
        string role)
    {
        role =
            role.Trim().ToLowerInvariant();

        if (role == "admin")
        {
            return;
        }

        if (role == "customer")
        {
            if (order.CustomerId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền truy cập Order này.");
            }

            return;
        }

        if (role == "seller")
        {
            bool ownsShop =
                await _unitOfWork.Shops
                    .AnyAsync(
                        x =>
                            x.Id == order.ShopId
                            && x.OwnerUserId == userId);

            if (!ownsShop)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền truy cập Order của Shop này.");
            }

            return;
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
            currentStatus.Trim().ToLowerInvariant();

        newStatus =
            newStatus.Trim().ToLowerInvariant();

        role =
            role.Trim().ToLowerInvariant();

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

        if (role == "seller")
        {
            bool valid =
                (
                    currentStatus ==
                        OrderStatuses.Pending
                    &&
                    newStatus ==
                        OrderStatuses.Confirmed
                )
                ||
                (
                    currentStatus ==
                        OrderStatuses.Confirmed
                    &&
                    newStatus ==
                        OrderStatuses.Processing
                )
                ||
                (
                    (
                        currentStatus ==
                            OrderStatuses.Pending
                        ||
                        currentStatus ==
                            OrderStatuses.Confirmed
                        ||
                        currentStatus ==
                            OrderStatuses.Processing
                    )
                    &&
                    newStatus ==
                        OrderStatuses.Cancelled
                );

            if (!valid)
            {
                throw new BadRequestException(
                    "Seller không được phép chuyển Order sang trạng thái này.");
            }

            return;
        }

        if (role == "admin")
        {
            bool valid =
                (
                    currentStatus ==
                        OrderStatuses.Pending
                    &&
                    (
                        newStatus ==
                            OrderStatuses.Confirmed
                        ||
                        newStatus ==
                            OrderStatuses.Cancelled
                    )
                )
                ||
                (
                    currentStatus ==
                        OrderStatuses.Confirmed
                    &&
                    (
                        newStatus ==
                            OrderStatuses.Processing
                        ||
                        newStatus ==
                            OrderStatuses.Cancelled
                    )
                )
                ||
                (
                    currentStatus ==
                        OrderStatuses.Processing
                    &&
                    newStatus ==
                        OrderStatuses.Cancelled
                );

            if (!valid)
            {
                throw new BadRequestException(
                    "Admin không được phép chuyển Order sang trạng thái này ở giai đoạn hiện tại.");
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
        BuildResponse(OrderModel order)
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
                        x.OrderId
                        == order.Id);

        result.Items =
            items.Select(
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

        // ================================================
        // STATUS LOGS
        // ================================================

        var logs =
            await _unitOfWork.OrderStatusLogs
                .FindAsync(
                    x =>
                        x.OrderId
                        == order.Id);

        result.StatusLogs =
            logs.Select(
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
                .OrderBy(x => x.CreatedAt)
                .ToList();

        return result;
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
}