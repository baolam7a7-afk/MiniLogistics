using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;
using OrderEntity = MiniLogistics.DAL.Models.Order;

namespace MiniLogistics.BLL.Services.QrPayment;

public class VietQrSettings
{
    public string BankBin { get; set; } = "970422";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public int ExpireMinutes { get; set; } = 15;
}

public class QrPaymentResponseDTO
{
    public long PaymentId { get; set; }
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentRef { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IQrPaymentService
{
    Task<QrPaymentResponseDTO> CreateOrGetAsync(long userId, long orderId);
    Task<QrPaymentResponseDTO> GetStatusAsync(long userId, long paymentId);
    Task<QrPaymentResponseDTO> ConfirmPaidAsync(long userId, long paymentId, bool isAdmin = false);
}

public class QrPaymentService : IQrPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly VietQrSettings _settings;

    public QrPaymentService(IUnitOfWork uow, IOptions<VietQrSettings> settings)
    {
        _uow = uow;
        _settings = settings.Value;
    }

    public async Task<QrPaymentResponseDTO> CreateOrGetAsync(long userId, long orderId)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("Đơn hàng không tồn tại.");

        if (order.CustomerId != userId)
            throw new ForbiddenException("Bạn không có quyền thanh toán đơn này.");

        if (order.PaymentMethod != "qr" && order.PaymentMethod != "QR")
        {
            // Cho phép chuyển pending COD → QR nếu chưa thanh toán.
            if (order.Status is not ("pending" or "awaiting_payment"))
                throw new BadRequestException("Đơn hàng không thể thanh toán QR ở trạng thái hiện tại.");

            order.PaymentMethod = "qr";
        }

        var paid = order.PaymentTransactions.FirstOrDefault(p =>
            p.Status == "paid" && p.Method.Equals("qr", StringComparison.OrdinalIgnoreCase));

        if (paid != null)
            return Map(order, paid);

        var now = DateTime.UtcNow;
        var pending = order.PaymentTransactions
            .Where(p =>
                p.Method.Equals("qr", StringComparison.OrdinalIgnoreCase) &&
                p.Status == "pending")
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();

        if (pending != null && pending.ExpiresAt.HasValue && pending.ExpiresAt > now)
            return Map(order, pending);

        if (pending != null)
        {
            pending.Status = "expired";
        }

        var paymentRef = $"ML{order.OrderCode}-{now:HHmmss}";
        var qrUrl = BuildVietQrUrl(order.Total, paymentRef);

        var payment = new PaymentTransaction
        {
            OrderId = order.Id,
            Provider = "vietqr",
            Method = "qr",
            Amount = order.Total,
            Status = "pending",
            PaymentRef = paymentRef,
            QrPayload = qrUrl,
            ProviderTxnId = paymentRef,
            ExpiresAt = now.AddMinutes(Math.Max(5, _settings.ExpireMinutes)),
            CreatedAt = now
        };

        await _uow.PaymentTransactions.AddAsync(payment);

        if (order.Status == "pending")
            order.Status = "awaiting_payment";

        await _uow.SaveChangesAsync();
        return Map(order, payment);
    }

    public async Task<QrPaymentResponseDTO> GetStatusAsync(long userId, long paymentId)
    {
        var payment = await _uow.PaymentTransactions.Query()
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new NotFoundException("Giao dịch không tồn tại.");

        if (payment.Order.CustomerId != userId)
            throw new ForbiddenException("Bạn không có quyền xem giao dịch này.");

        if (payment.Status == "pending" &&
            payment.ExpiresAt.HasValue &&
            payment.ExpiresAt < DateTime.UtcNow)
        {
            payment.Status = "expired";
            await _uow.SaveChangesAsync();
        }

        return Map(payment.Order, payment);
    }

    public async Task<QrPaymentResponseDTO> ConfirmPaidAsync(
        long userId,
        long paymentId,
        bool isAdmin = false)
    {
        var payment = await _uow.PaymentTransactions.Query()
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new NotFoundException("Giao dịch không tồn tại.");

        if (!isAdmin && payment.Order.CustomerId != userId)
            throw new ForbiddenException("Bạn không có quyền xác nhận giao dịch này.");

        if (payment.Status == "paid")
            return Map(payment.Order, payment);

        if (payment.Status == "expired")
            throw new BadRequestException("QR đã hết hạn. Vui lòng tạo lại QR thanh toán.");

        if (payment.Status != "pending")
            throw new BadRequestException($"Không thể xác nhận giao dịch ở trạng thái '{payment.Status}'.");

        var now = DateTime.UtcNow;
        payment.Status = "paid";
        payment.PaidAt = now;

        // Đóng các QR pending khác của cùng order.
        var otherPending = await _uow.PaymentTransactions.Query()
            .Where(p =>
                p.OrderId == payment.OrderId &&
                p.Id != payment.Id &&
                p.Status == "pending")
            .ToListAsync();

        foreach (var p in otherPending)
            p.Status = "cancelled";

        var orderStatus = payment.Order.Status?.Trim().ToLowerInvariant();
        if (orderStatus is "awaiting_payment" or "paid")
        {
            payment.Order.Status = "pending";
            payment.Order.UpdatedAt = now;
            await _uow.OrderStatusLogs.AddAsync(new OrderStatusLog
            {
                OrderId = payment.Order.Id,
                FromStatus = orderStatus,
                ToStatus = "pending",
                Message = "Đã thanh toán. Chờ shop xác nhận.",
                CreatedByUserId = userId,
                CreatedAt = now
            });
        }

        await RemovePaidItemsFromCartAsync(payment.Order);
        await _uow.SaveChangesAsync();
        return Map(payment.Order, payment);
    }

    private async Task RemovePaidItemsFromCartAsync(OrderEntity order)
    {
        var items = await _uow.OrderItems.Query()
            .Where(item => item.OrderId == order.Id)
            .ToListAsync();
        if (items.Count == 0)
        {
            return;
        }

        var cart = await _uow.Carts.Query()
            .FirstOrDefaultAsync(row => row.UserId == order.CustomerId);
        if (cart == null)
        {
            return;
        }

        var variantIds = items.Select(item => item.VariantId).ToHashSet();
        var lines = await _uow.CartItems.Query()
            .Where(line => line.CartId == cart.Id && variantIds.Contains(line.VariantId))
            .ToListAsync();

        foreach (var line in lines)
        {
            var ordered = items
                .Where(item => item.VariantId == line.VariantId)
                .Sum(item => item.Quantity);
            if (ordered >= line.Quantity)
            {
                _uow.CartItems.Delete(line);
            }
            else if (ordered > 0)
            {
                line.Quantity -= ordered;
                line.UpdatedAt = DateTime.UtcNow;
            }
        }

        cart.UpdatedAt = DateTime.UtcNow;
    }

    private string BuildVietQrUrl(decimal amount, string addInfo)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccountNumber) ||
            string.IsNullOrWhiteSpace(_settings.AccountName))
        {
            throw new BadRequestException("Chưa cấu hình tài khoản VietQR.");
        }

        var bank = Uri.EscapeDataString(_settings.BankBin);
        var account = Uri.EscapeDataString(_settings.AccountNumber);
        var name = Uri.EscapeDataString(_settings.AccountName);
        var info = Uri.EscapeDataString(addInfo);
        var amountInt = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);

        return $"https://img.vietqr.io/image/{bank}-{account}-compact2.png?amount={amountInt}&addInfo={info}&accountName={name}";
    }

    private QrPaymentResponseDTO Map(OrderEntity order, PaymentTransaction payment) => new()
    {
        PaymentId = payment.Id,
        OrderId = order.Id,
        OrderCode = order.OrderCode,
        Amount = payment.Amount,
        Status = payment.Status,
        PaymentRef = payment.PaymentRef ?? string.Empty,
        QrImageUrl = payment.QrPayload ?? string.Empty,
        AccountName = _settings.AccountName,
        AccountNumber = _settings.AccountNumber,
        ExpiresAt = payment.ExpiresAt,
        PaidAt = payment.PaidAt,
        CreatedAt = payment.CreatedAt
    };
}
