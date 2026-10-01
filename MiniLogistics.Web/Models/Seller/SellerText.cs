using System.Globalization;
using System.Text;

namespace MiniLogistics.Web.Models.Seller;

public static class SellerText
{
    public static string Money(decimal value) =>
        string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} ₫", value);

    public static string When(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Utc
            ? value.ToLocalTime()
            : value;
        return local.ToString("dd/MM/yyyy HH:mm");
    }

    public static string OrderStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "pending" => "Chờ xác nhận",
        "confirmed" => "Đã xác nhận",
        "processing" => "Đang xử lý",
        "shipping" => "Đang giao",
        "delivered" => "Đã giao",
        "cancelled" => "Đã hủy",
        "awaiting_payment" => "Chờ thanh toán",
        "paid" => "Đã thanh toán",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string ProductStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "active" => "Đang bán",
        "draft" => "Nháp",
        "inactive" => "Ngừng bán",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string ShopStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "pending" => "Chờ duyệt",
        "approved" => "Đã duyệt",
        "rejected" => "Bị từ chối",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string VoucherStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "active" => "Đang bật",
        "inactive" => "Đã tắt",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string PayoutStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "requested" => "Chờ duyệt",
        "approved" => "Đã duyệt",
        "rejected" => "Từ chối",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string ReturnStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "requested" => "Chờ xử lý",
        "approved" => "Đã duyệt",
        "rejected" => "Từ chối",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string TicketStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "open" => "Mở",
        "in_progress" => "Đang xử lý",
        "resolved" => "Đã xử lý",
        "closed" => "Đã đóng",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousDash = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var current = ch == 'đ' ? 'd' : ch;
            if (char.IsLetterOrDigit(current))
            {
                builder.Append(current);
                previousDash = false;
            }
            else if (!previousDash && builder.Length > 0)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
