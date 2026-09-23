using MiniLogistics.BLL.DTOs.OrderVoucher;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using OrderModel = MiniLogistics.DAL.Models.Order;
using OrderVoucherModel = MiniLogistics.DAL.Models.OrderVoucher;
using VoucherModel = MiniLogistics.DAL.Models.Voucher;

namespace MiniLogistics.BLL.Services.OrderVoucher;

public class OrderVoucherService : IOrderVoucherService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderVoucherService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // APPLY VOUCHER
    // =====================================================

    public async Task<OrderVoucherResponseDTO> ApplyAsync(
        long customerId,
        ApplyVoucherDTO request)
    {
        // =================================================
        // 1. VALIDATE REQUEST
        // =================================================

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new BadRequestException(
                "Voucher Code không được để trống.");
        }


        // =================================================
        // 2. TRANSACTION
        // =================================================

        return await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                // =========================================
                // 3. TÌM ORDER
                // =========================================

                var order =
                    await _unitOfWork.Orders
                        .GetByIdAsync(request.OrderId);

                if (order == null)
                {
                    throw new NotFoundException(
                        "Order không tồn tại.");
                }


                // =========================================
                // 4. CHECK CUSTOMER
                // =========================================

                if (order.CustomerId != customerId)
                {
                    throw new ForbiddenException(
                        "Bạn không có quyền sử dụng Voucher cho Order này.");
                }


                // =========================================
                // 5. CHECK ORDER STATUS
                // =========================================

                if (order.Status != "pending")
                {
                    throw new BadRequestException(
                        "Chỉ có thể áp Voucher cho Order đang pending.");
                }


                // =========================================
                // 6. CHECK ORDER ĐÃ CÓ VOUCHER
                // =========================================

                var existingOrderVouchers =
                    await _unitOfWork.OrderVouchers
                        .FindAsync(
                            x =>
                                x.OrderId == order.Id);

                if (existingOrderVouchers.Any())
                {
                    throw new BadRequestException(
                        "Order này đã có Voucher.");
                }


                // =========================================
                // 7. CHUẨN HÓA CODE
                // =========================================

                string code =
                    request.Code
                        .Trim()
                        .ToUpperInvariant();


                // =========================================
                // 8. TÌM VOUCHER
                // =========================================

                var vouchers =
                    await _unitOfWork.Vouchers
                        .FindAsync(
                            x =>
                                x.Code == code);

                var voucher =
                    vouchers.FirstOrDefault();

                if (voucher == null)
                {
                    throw new NotFoundException(
                        "Voucher không tồn tại.");
                }


                // =========================================
                // 9. CHECK STATUS
                // =========================================

                if (voucher.Status != "active")
                {
                    throw new BadRequestException(
                        "Voucher không active.");
                }


                // =========================================
                // 10. CHECK THỜI GIAN
                // =========================================

                var now =
                    DateTime.UtcNow;

                if (now < voucher.StartAt)
                {
                    throw new BadRequestException(
                        "Voucher chưa bắt đầu hiệu lực.");
                }

                if (now > voucher.EndAt)
                {
                    throw new BadRequestException(
                        "Voucher đã hết hạn.");
                }


                // =========================================
                // 11. CHECK USAGE LIMIT
                // =========================================

                if (voucher.UsageLimit.HasValue &&
                    voucher.UsedCount >=
                    voucher.UsageLimit.Value)
                {
                    throw new BadRequestException(
                        "Voucher đã hết lượt sử dụng.");
                }


                // =========================================
                // 12. CHECK SCOPE
                // =========================================

                if (voucher.Scope != "platform" &&
                    voucher.Scope != "shop")
                {
                    throw new BadRequestException(
                        "Scope của Voucher không hợp lệ.");
                }


                // =========================================
                // 13. CHECK SHOP
                // =========================================

                if (voucher.Scope == "shop")
                {
                    if (!voucher.ShopId.HasValue)
                    {
                        throw new BadRequestException(
                            "Voucher Shop chưa được cấu hình ShopId.");
                    }

                    if (voucher.ShopId.Value != order.ShopId)
                    {
                        throw new BadRequestException(
                            "Voucher không áp dụng cho Shop của Order.");
                    }
                }


                // =========================================
                // 14. CHECK MIN ORDER VALUE
                // =========================================

                if (voucher.MinOrderValue.HasValue &&
                    order.Subtotal <
                    voucher.MinOrderValue.Value)
                {
                    throw new BadRequestException(
                        $"Order chưa đạt giá trị tối thiểu " +
                        $"{voucher.MinOrderValue.Value:N0} VND.");
                }


                // =========================================
                // 15. CALCULATE DISCOUNT
                // =========================================

                decimal discountAmount;


                // -----------------------------------------
                // PERCENT
                // -----------------------------------------

                if (voucher.DiscountType == "percent")
                {
                    discountAmount =
                        order.Subtotal
                        * voucher.DiscountValue
                        / 100m;

                    if (voucher.MaxDiscount.HasValue &&
                        discountAmount >
                        voucher.MaxDiscount.Value)
                    {
                        discountAmount =
                            voucher.MaxDiscount.Value;
                    }
                }


                // -----------------------------------------
                // FIXED AMOUNT
                // -----------------------------------------

                else if (voucher.DiscountType == "amount")
                {
                    discountAmount =
                        voucher.DiscountValue;
                }


                // -----------------------------------------
                // INVALID TYPE
                // -----------------------------------------

                else
                {
                    throw new BadRequestException(
                        "DiscountType của Voucher không hợp lệ.");
                }


                // =========================================
                // 16. KHÔNG CHO GIẢM QUÁ SUBTOTAL
                // =========================================

                if (discountAmount >
                    order.Subtotal)
                {
                    discountAmount =
                        order.Subtotal;
                }


                if (discountAmount <= 0)
                {
                    throw new BadRequestException(
                        "DiscountAmount phải lớn hơn 0.");
                }


                // =========================================
                // 17. CREATE ORDER VOUCHER
                // =========================================

                var orderVoucher =
                    new OrderVoucherModel
                    {
                        OrderId =
                            order.Id,

                        VoucherId =
                            voucher.Id,

                        CodeSnapshot =
                            voucher.Code,

                        DiscountAmount =
                            discountAmount
                    };


                await _unitOfWork.OrderVouchers
                    .AddAsync(orderVoucher);


                // =========================================
                // 18. UPDATE ORDER DISCOUNT
                // =========================================

                order.DiscountTotal =
                    discountAmount;


                // =========================================
                // 19. UPDATE ORDER TOTAL
                // =========================================

                order.Total =
    order.Subtotal
    + order.ShippingFee
    - order.DiscountTotal;

if (order.Total < 0)
{
    order.Total = 0;
}

order.UpdatedAt =
    DateTime.UtcNow;

_unitOfWork.Orders
    .Update(order);


// =========================================
// UPDATE PAYMENT AMOUNT
// =========================================

var payments =
    await _unitOfWork.PaymentTransactions
        .FindAsync(
            x =>
                x.OrderId == order.Id &&
                x.Status == "pending");

foreach (var payment in payments)
{
    payment.Amount =
        order.Total;

    _unitOfWork.PaymentTransactions
        .Update(payment);
}


                // =========================================
                // 20. UPDATE VOUCHER USED COUNT
                // =========================================

                voucher.UsedCount++;

                _unitOfWork.Vouchers
                    .Update(voucher);


                // =========================================
                // 21. SAVE
                // =========================================

                await _unitOfWork.SaveChangesAsync();


                // =========================================
                // 22. RESPONSE
                // =========================================

                return MapToResponse(
                    orderVoucher);
            });
    }


    // =====================================================
    // GET VOUCHER BY ORDER
    // =====================================================

    public async Task<IEnumerable<OrderVoucherResponseDTO>>
        GetByOrderIdAsync(
            long customerId,
            long orderId)
    {
        // =================================================
        // 1. TÌM ORDER
        // =================================================

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(orderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }


        // =================================================
        // 2. CHECK CUSTOMER
        // =================================================

        if (order.CustomerId != customerId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem Voucher của Order này.");
        }


        // =================================================
        // 3. LẤY ORDER VOUCHER
        // =================================================

        var orderVouchers =
            await _unitOfWork.OrderVouchers
                .FindAsync(
                    x =>
                        x.OrderId == orderId);


        // =================================================
        // 4. RESPONSE
        // =================================================

        return orderVouchers
            .OrderBy(x => x.Id)
            .Select(
                MapToResponse)
            .ToList();
    }


    // =====================================================
    // REMOVE VOUCHER
    // =====================================================

    public async Task RemoveAsync(
        long customerId,
        long orderVoucherId)
    {
        // =================================================
        // 1. TÌM ORDER VOUCHER
        // =================================================

        var orderVoucher =
            await _unitOfWork.OrderVouchers
                .GetByIdAsync(orderVoucherId);

        if (orderVoucher == null)
        {
            throw new NotFoundException(
                "OrderVoucher không tồn tại.");
        }


        // =================================================
        // 2. TÌM ORDER
        // =================================================

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(
                    orderVoucher.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                "Order không tồn tại.");
        }


        // =================================================
        // 3. CHECK CUSTOMER
        // =================================================

        if (order.CustomerId != customerId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xóa Voucher khỏi Order này.");
        }


        // =================================================
        // 4. CHECK ORDER STATUS
        // =================================================

        if (order.Status != "pending")
        {
            throw new BadRequestException(
                "Chỉ có thể xóa Voucher khỏi Order pending.");
        }


        // =================================================
        // 5. TÌM VOUCHER
        // =================================================

        var voucher =
            await _unitOfWork.Vouchers
                .GetByIdAsync(
                    orderVoucher.VoucherId);


        // =================================================
        // 6. XÓA ORDER VOUCHER
        // =================================================

        _unitOfWork.OrderVouchers
            .Delete(orderVoucher);


        // =================================================
        // 7. RESET DISCOUNT
        // =================================================

        order.DiscountTotal = 0m;


        // =================================================
        // 8. TÍNH LẠI TOTAL
        // =================================================

        order.Total =
            order.Subtotal
            + order.ShippingFee;


        if (order.Total < 0)
        {
            order.Total = 0;
        }


        order.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Orders
            .Update(order);


        // =================================================
        // 9. GIẢM USED COUNT
        // =================================================

        if (voucher != null &&
            voucher.UsedCount > 0)
        {
            voucher.UsedCount--;

            _unitOfWork.Vouchers
                .Update(voucher);
        }


        // =================================================
        // 10. SAVE
        // =================================================

        await _unitOfWork.SaveChangesAsync();
    }


    // =====================================================
    // MAP RESPONSE
    // =====================================================

    private OrderVoucherResponseDTO MapToResponse(
        OrderVoucherModel orderVoucher)
    {
        return new OrderVoucherResponseDTO
        {
            Id =
                orderVoucher.Id,

            OrderId =
                orderVoucher.OrderId,

            VoucherId =
                orderVoucher.VoucherId,

            CodeSnapshot =
                orderVoucher.CodeSnapshot,

            DiscountAmount =
                orderVoucher.DiscountAmount
        };
    }
}