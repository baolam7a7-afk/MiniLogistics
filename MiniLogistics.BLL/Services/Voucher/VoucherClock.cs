namespace MiniLogistics.BLL.Services.Voucher;

public static class VoucherClock
{
    public static bool IsOpen(DateTime start, DateTime end, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return HasStarted(start, now) && !HasEnded(end, now);
    }

    public static bool HasStarted(DateTime start, DateTime utcNow)
    {
        var wall = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
        if (DateTime.SpecifyKind(wall, DateTimeKind.Utc) <= utcNow)
        {
            return true;
        }

        return TimeZoneInfo.ConvertTimeToUtc(wall, TimeZoneInfo.Local) <= utcNow;
    }

    public static bool HasEnded(DateTime end, DateTime utcNow)
    {
        var wall = DateTime.SpecifyKind(end, DateTimeKind.Unspecified);
        var asUtc = DateTime.SpecifyKind(wall, DateTimeKind.Utc);
        var asLocal = TimeZoneInfo.ConvertTimeToUtc(wall, TimeZoneInfo.Local);
        return asUtc < utcNow && asLocal < utcNow;
    }
}
