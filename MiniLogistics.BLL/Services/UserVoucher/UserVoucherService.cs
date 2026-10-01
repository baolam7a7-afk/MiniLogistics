using MiniLogistics.BLL.DTOs.Voucher;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace MiniLogistics.BLL.Services.UserVoucher;

public interface IUserVoucherService
{
    Task<IEnumerable<UserVoucherResponseDTO>> GetMyVouchersAsync(long userId);
    Task<IEnumerable<VoucherResponseDTO>> GetClaimableAsync(long userId, long? shopId = null);
    Task<UserVoucherResponseDTO> ClaimAsync(long userId, long voucherId);
}

public class UserVoucherResponseDTO
{
    public long Id { get; set; }
    public long VoucherId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Scope { get; set; } = string.Empty;
    public long? ShopId { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinOrderValue { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ClaimedAt { get; set; }
}

public class UserVoucherService : IUserVoucherService
{
    private readonly IUnitOfWork _uow;

    public UserVoucherService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<UserVoucherResponseDTO>> GetMyVouchersAsync(long userId)
    {
        var now = DateTime.UtcNow;
        var items = await _uow.UserVouchers.Query()
            .AsNoTracking()
            .Include(x => x.Voucher)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ClaimedAt)
            .ToListAsync();

        foreach (var item in items.Where(x =>
                     x.Status == "available" &&
                     (x.Voucher.EndAt < now || x.Voucher.Status != "active")))
        {
            item.Status = "expired";
        }

        return items.Select(Map);
    }

    public async Task<IEnumerable<VoucherResponseDTO>> GetClaimableAsync(long userId, long? shopId = null)
    {
        var now = DateTime.UtcNow;
        var claimedIds = await _uow.UserVouchers.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.VoucherId)
            .ToListAsync();

        var query = _uow.Vouchers.Query()
            .AsNoTracking()
            .Where(v =>
                v.Status == "active" &&
                v.StartAt <= now &&
                v.EndAt >= now &&
                !claimedIds.Contains(v.Id) &&
                (v.UsageLimit == null || v.UsedCount < v.UsageLimit));

        if (shopId.HasValue)
        {
            query = query.Where(v =>
                v.Scope == "platform" ||
                (v.Scope == "shop" && v.ShopId == shopId));
        }

        var list = await query.OrderByDescending(v => v.Id).Take(50).ToListAsync();

        return list.Select(v => new VoucherResponseDTO
        {
            Id = v.Id,
            Scope = v.Scope,
            ShopId = v.ShopId,
            Code = v.Code,
            Name = v.Name,
            Description = v.Description,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MaxDiscount = v.MaxDiscount,
            MinOrderValue = v.MinOrderValue,
            UsageLimit = v.UsageLimit,
            UsedCount = v.UsedCount,
            StartAt = v.StartAt,
            EndAt = v.EndAt,
            Status = v.Status,
            CreatedAt = v.CreatedAt
        });
    }

    public async Task<UserVoucherResponseDTO> ClaimAsync(long userId, long voucherId)
    {
        var now = DateTime.UtcNow;

        var voucher = await _uow.Vouchers.GetByIdAsync(voucherId)
            ?? throw new NotFoundException("Voucher không tồn tại.");

        if (voucher.Status != "active")
            throw new BadRequestException("Voucher không còn hiệu lực.");

        if (voucher.StartAt > now)
            throw new BadRequestException("Voucher chưa đến thời gian bắt đầu.");

        if (voucher.EndAt < now)
            throw new BadRequestException("Voucher đã hết hạn.");

        if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
            throw new BadRequestException("Voucher đã hết số lượng.");

        var exists = await _uow.UserVouchers.AnyAsync(x =>
            x.UserId == userId && x.VoucherId == voucherId);

        if (exists)
            throw new BadRequestException("Bạn đã nhận voucher này rồi.");

        var entity = new DAL.Models.UserVoucher
        {
            UserId = userId,
            VoucherId = voucherId,
            ClaimedAt = now,
            Status = "available"
        };

        await _uow.UserVouchers.AddAsync(entity);
        await _uow.SaveChangesAsync();

        entity.Voucher = voucher;
        return Map(entity);
    }

    private static UserVoucherResponseDTO Map(DAL.Models.UserVoucher x) => new()
    {
        Id = x.Id,
        VoucherId = x.VoucherId,
        Code = x.Voucher?.Code ?? string.Empty,
        Name = x.Voucher?.Name,
        Scope = x.Voucher?.Scope ?? string.Empty,
        ShopId = x.Voucher?.ShopId,
        DiscountType = x.Voucher?.DiscountType ?? string.Empty,
        DiscountValue = x.Voucher?.DiscountValue ?? 0,
        MaxDiscount = x.Voucher?.MaxDiscount,
        MinOrderValue = x.Voucher?.MinOrderValue,
        StartAt = x.Voucher?.StartAt ?? default,
        EndAt = x.Voucher?.EndAt ?? default,
        Status = x.Status,
        ClaimedAt = x.ClaimedAt
    };
}
