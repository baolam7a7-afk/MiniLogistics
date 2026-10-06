using Microsoft.EntityFrameworkCore;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services.Referral;

public interface IReferralService
{
    Task<ReferralCodeDTO> GetOrCreateMyCodeAsync(long userId);
    Task ApplyAsync(long newUserId, long shopId, string? code, string? ip, string? userAgent);
    Task<ReferralPolicyDTO> GetPolicyAsync();
    Task<ReferralPolicyDTO> UpdatePolicyAsync(UpdateReferralPolicyDTO request);
    Task<ReferralSummaryDTO> GetSummaryAsync();
    Task<IReadOnlyList<SellerReferralDTO>> ListAsync(string? status);
    Task<SellerReferralDTO> EvaluateAsync(long referralId, long adminUserId);
    Task<SellerReferralDTO> ApprovePayoutAsync(long referralId, long adminUserId);
    Task<SellerReferralDTO> RejectAsync(long referralId, long adminUserId, string reason);
    Task<SellerReferralDTO> FlagAsync(long referralId, long adminUserId, string reason);
    Task<SellerReferralDTO> UnflagAsync(long referralId, long adminUserId);
    Task<int> SettleDueAsync(long adminUserId);
}

public class ReferralCodeDTO
{
    public string Code { get; set; } = "";
}

public class ReferralPolicyDTO
{
    public string RewardMode { get; set; } = "flat";
    public decimal FlatAmount { get; set; }
    public decimal SharePercent { get; set; }
    public decimal PlatformFeePercent { get; set; }
    public int MinProductCount { get; set; }
    public bool RequireApprovedShop { get; set; }
    public bool RequireFirstOrder { get; set; }
    public int HoldingDays { get; set; }
    public string PayoutMode { get; set; } = "manual";
    public bool IsEnabled { get; set; }
}

public class UpdateReferralPolicyDTO
{
    public string RewardMode { get; set; } = "flat";
    public decimal FlatAmount { get; set; }
    public decimal SharePercent { get; set; }
    public decimal PlatformFeePercent { get; set; }
    public int MinProductCount { get; set; }
    public bool RequireApprovedShop { get; set; }
    public bool RequireFirstOrder { get; set; }
    public int HoldingDays { get; set; }
    public string PayoutMode { get; set; } = "manual";
    public bool IsEnabled { get; set; }
}

public class ReferralSummaryDTO
{
    public int ReferredShopCount { get; set; }
    public int PendingCount { get; set; }
    public int FlaggedCount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal HeldAmount { get; set; }
    public decimal FirstOrderRevenue { get; set; }
    public decimal CostPerShop { get; set; }
    public decimal Roi { get; set; }
}

public class SellerReferralDTO
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ReferrerName { get; set; }
    public string? ReferredShopName { get; set; }
    public string? FraudFlags { get; set; }
    public decimal RewardAmount { get; set; }
    public DateTime? QualifiedAt { get; set; }
    public DateTime? PayableAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RewardStatus { get; set; }
}

public class ReferralService : IReferralService
{
    private readonly IUnitOfWork _uow;

    public ReferralService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ReferralCodeDTO> GetOrCreateMyCodeAsync(long userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        if (string.IsNullOrWhiteSpace(user.ReferralCode))
        {
            user.ReferralCode = await NewCodeAsync();
            user.UpdatedAt = DateTime.UtcNow;
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
        }

        return new ReferralCodeDTO { Code = user.ReferralCode };
    }

    public async Task ApplyAsync(long newUserId, long shopId, string? code, string? ip, string? userAgent)
    {
        var policy = await EnsurePolicyAsync();
        if (!policy.IsEnabled || string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var normalized = code.Trim().ToUpperInvariant();
        var referrer = await _uow.Users.Query()
            .FirstOrDefaultAsync(user => user.ReferralCode == normalized)
            ?? throw new BadRequestException("Mã giới thiệu không tồn tại.");

        if (referrer.Id == newUserId)
        {
            throw new BadRequestException("Không thể dùng mã giới thiệu của chính mình.");
        }

        var duplicated = await _uow.SellerReferrals.Query()
            .AnyAsync(item => item.ReferredShopId == shopId);
        if (duplicated)
        {
            throw new BadRequestException("Shop này đã gắn mã giới thiệu.");
        }

        var flags = await DetectFraudAsync(referrer.Id, newUserId, ip, userAgent);
        var referral = new SellerReferral
        {
            ReferrerUserId = referrer.Id,
            ReferredUserId = newUserId,
            ReferredShopId = shopId,
            Code = normalized,
            Status = flags.Count > 0 ? "flagged" : "pending",
            SignupIp = Trim(ip, 64),
            DeviceHint = Trim(userAgent, 300),
            FraudFlags = flags.Count == 0 ? null : string.Join(",", flags),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.SellerReferrals.AddAsync(referral);
        await _uow.SaveChangesAsync();
    }

    public async Task<ReferralPolicyDTO> GetPolicyAsync() => MapPolicy(await EnsurePolicyAsync());

    public async Task<ReferralPolicyDTO> UpdatePolicyAsync(UpdateReferralPolicyDTO request)
    {
        var mode = request.RewardMode?.Trim().ToLowerInvariant();
        var payout = request.PayoutMode?.Trim().ToLowerInvariant();
        if (mode is not ("flat" or "percent"))
        {
            throw new BadRequestException("Kiểu thưởng phải là flat hoặc percent.");
        }

        if (payout is not ("manual" or "auto"))
        {
            throw new BadRequestException("Kiểu chi trả phải là manual hoặc auto.");
        }

        if (request.FlatAmount < 0 || request.SharePercent < 0 || request.PlatformFeePercent < 0)
        {
            throw new BadRequestException("Mức thưởng không được âm.");
        }

        if (request.SharePercent > 100 || request.PlatformFeePercent > 100)
        {
            throw new BadRequestException("Phần trăm không được vượt quá 100.");
        }

        if (request.HoldingDays < 0 || request.MinProductCount < 0)
        {
            throw new BadRequestException("Số ngày giữ thưởng và số sản phẩm không được âm.");
        }

        var policy = await EnsurePolicyAsync();
        policy.RewardMode = mode;
        policy.FlatAmount = request.FlatAmount;
        policy.SharePercent = request.SharePercent;
        policy.PlatformFeePercent = request.PlatformFeePercent;
        policy.MinProductCount = request.MinProductCount;
        policy.RequireApprovedShop = request.RequireApprovedShop;
        policy.RequireFirstOrder = request.RequireFirstOrder;
        policy.HoldingDays = request.HoldingDays;
        policy.PayoutMode = payout;
        policy.IsEnabled = request.IsEnabled;
        policy.UpdatedAt = DateTime.UtcNow;
        _uow.ReferralPolicies.Update(policy);
        await _uow.SaveChangesAsync();
        return MapPolicy(policy);
    }

    public async Task<ReferralSummaryDTO> GetSummaryAsync()
    {
        var rows = await _uow.SellerReferrals.Query().AsNoTracking().ToListAsync();
        var paid = await _uow.ReferralRewardTransactions.Query()
            .AsNoTracking()
            .Where(item => item.Status == "paid")
            .SumAsync(item => (decimal?)item.Amount) ?? 0;
        var held = await _uow.ReferralRewardTransactions.Query()
            .AsNoTracking()
            .Where(item => item.Status == "held")
            .SumAsync(item => (decimal?)item.Amount) ?? 0;

        var shopIds = rows.Select(item => item.ReferredShopId).ToList();
        var revenue = 0m;
        if (shopIds.Count > 0)
        {
            var orders = await _uow.Orders.Query()
                .AsNoTracking()
                .Where(order => shopIds.Contains(order.ShopId) && order.Status == "delivered")
                .Select(order => new { order.ShopId, order.Total, order.PlacedAt })
                .ToListAsync();
            revenue = orders
                .GroupBy(order => order.ShopId)
                .Select(group => group.OrderBy(order => order.PlacedAt).First().Total)
                .Sum();
        }

        var shops = rows.Count;
        return new ReferralSummaryDTO
        {
            ReferredShopCount = shops,
            PendingCount = rows.Count(item => item.Status == "pending"),
            FlaggedCount = rows.Count(item => item.Status == "flagged"),
            PaidAmount = paid,
            HeldAmount = held,
            FirstOrderRevenue = revenue,
            CostPerShop = shops == 0 ? 0 : Math.Round(paid / shops, 2),
            Roi = paid <= 0 ? 0 : Math.Round(revenue / paid, 2)
        };
    }

    public async Task<IReadOnlyList<SellerReferralDTO>> ListAsync(string? status)
    {
        var query = _uow.SellerReferrals.Query()
            .AsNoTracking()
            .Include(item => item.ReferrerUser)
            .Include(item => item.ReferredShop)
            .Include(item => item.Rewards)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            query = query.Where(item => item.Status == normalized);
        }

        var rows = await query.OrderByDescending(item => item.CreatedAt).Take(200).ToListAsync();
        return rows.Select(MapReferral).ToList();
    }

    public async Task<SellerReferralDTO> EvaluateAsync(long referralId, long adminUserId)
    {
        var referral = await LoadTrackedAsync(referralId);
        await RefreshQualificationAsync(referral);
        if (referral.Status is "pending" or "flagged" &&
            referral.PayableAt != null &&
            referral.PayableAt <= DateTime.UtcNow &&
            (await EnsurePolicyAsync()).PayoutMode == "auto" &&
            referral.Status != "flagged")
        {
            await PayAsync(referral, adminUserId);
        }

        await _uow.SaveChangesAsync();
        return await GetDtoAsync(referralId);
    }

    public Task<SellerReferralDTO> ApprovePayoutAsync(long referralId, long adminUserId) =>
        _uow.ExecuteInTransactionAsync(async () =>
        {
            var referral = await LoadTrackedAsync(referralId);
            await RefreshQualificationAsync(referral);
            await PayAsync(referral, adminUserId);
            await _uow.SaveChangesAsync();
            return await GetDtoAsync(referralId);
        });

    public async Task<SellerReferralDTO> RejectAsync(long referralId, long adminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BadRequestException("Cần nhập lý do từ chối.");
        }

        var referral = await LoadTrackedAsync(referralId);
        if (referral.Status is "approved" or "rejected")
        {
            throw new BadRequestException("Lượt giới thiệu này đã kết thúc.");
        }

        referral.Status = "rejected";
        referral.ReviewNote = reason.Trim();
        referral.ReviewedByUserId = adminUserId;
        referral.UpdatedAt = DateTime.UtcNow;
        await CancelHoldAsync(referral.Id);
        await _uow.SaveChangesAsync();
        return await GetDtoAsync(referralId);
    }

    public async Task<SellerReferralDTO> FlagAsync(long referralId, long adminUserId, string reason)
    {
        var referral = await LoadTrackedAsync(referralId);
        if (referral.Status == "approved")
        {
            throw new BadRequestException("Không gắn cờ lượt đã chi thưởng.");
        }

        referral.Status = "flagged";
        referral.ReviewNote = string.IsNullOrWhiteSpace(reason) ? referral.ReviewNote : reason.Trim();
        referral.ReviewedByUserId = adminUserId;
        referral.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return await GetDtoAsync(referralId);
    }

    public async Task<SellerReferralDTO> UnflagAsync(long referralId, long adminUserId)
    {
        var referral = await LoadTrackedAsync(referralId);
        if (referral.Status != "flagged")
        {
            throw new BadRequestException("Lượt này không ở trạng thái nghi vấn.");
        }

        referral.Status = "pending";
        referral.ReviewedByUserId = adminUserId;
        referral.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return await GetDtoAsync(referralId);
    }

    public async Task<int> SettleDueAsync(long adminUserId)
    {
        var ids = await _uow.SellerReferrals.Query()
            .Where(item => item.Status == "pending")
            .Select(item => item.Id)
            .ToListAsync();
        var paid = 0;
        foreach (var id in ids)
        {
            var before = await _uow.SellerReferrals.Query().AsNoTracking()
                .Where(item => item.Id == id)
                .Select(item => item.Status)
                .FirstAsync();
            await EvaluateAsync(id, adminUserId);
            var after = await _uow.SellerReferrals.Query().AsNoTracking()
                .Where(item => item.Id == id)
                .Select(item => item.Status)
                .FirstAsync();
            if (before != "approved" && after == "approved")
            {
                paid++;
            }
        }

        return paid;
    }

    private async Task RefreshQualificationAsync(SellerReferral referral)
    {
        if (referral.Status is "approved" or "rejected")
        {
            return;
        }

        var policy = await EnsurePolicyAsync();
        var shop = await _uow.Shops.GetByIdAsync(referral.ReferredShopId)
            ?? throw new NotFoundException("Shop được giới thiệu không còn tồn tại.");
        var productCount = await _uow.Products.Query().CountAsync(product => product.ShopId == shop.Id);
        var firstOrder = await _uow.Orders.Query()
            .Where(order => order.ShopId == shop.Id && order.Status == "delivered")
            .OrderBy(order => order.PlacedAt)
            .Select(order => (decimal?)order.Total)
            .FirstOrDefaultAsync();

        var enoughProducts = productCount >= policy.MinProductCount;
        var approved = !policy.RequireApprovedShop || shop.Status == "approved";
        var hasOrder = !policy.RequireFirstOrder || firstOrder != null;
        if (!(enoughProducts && approved && hasOrder))
        {
            referral.QualifiedAt = null;
            referral.PayableAt = null;
            referral.RewardAmount = 0;
            referral.UpdatedAt = DateTime.UtcNow;
            return;
        }

        referral.QualifiedAt ??= DateTime.UtcNow;
        referral.PayableAt ??= referral.QualifiedAt.Value.AddDays(policy.HoldingDays);
        referral.RewardAmount = policy.RewardMode == "percent"
            ? Math.Round((firstOrder ?? 0) * policy.PlatformFeePercent / 100m * policy.SharePercent / 100m, 2)
            : policy.FlatAmount;
        referral.UpdatedAt = DateTime.UtcNow;

        var hold = await _uow.ReferralRewardTransactions.Query()
            .FirstOrDefaultAsync(item => item.SellerReferralId == referral.Id);
        if (hold == null && referral.RewardAmount > 0)
        {
            await _uow.ReferralRewardTransactions.AddAsync(new ReferralRewardTransaction
            {
                SellerReferralId = referral.Id,
                Amount = referral.RewardAmount,
                Status = "held",
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (hold is { Status: "held" })
        {
            hold.Amount = referral.RewardAmount;
        }
    }

    private async Task PayAsync(SellerReferral referral, long adminUserId)
    {
        if (referral.Status is "approved" or "rejected")
        {
            throw new BadRequestException("Lượt giới thiệu này đã kết thúc.");
        }

        if (referral.Status == "flagged")
        {
            throw new BadRequestException("Lượt đang nghi gian lận. Hãy xử lý cờ trước khi chi.");
        }

        if (referral.QualifiedAt == null || referral.PayableAt == null)
        {
            throw new BadRequestException("Shop chưa đủ điều kiện nhận thưởng.");
        }

        if (referral.PayableAt > DateTime.UtcNow)
        {
            throw new BadRequestException("Thưởng vẫn trong thời gian giữ, chưa được chi.");
        }

        if (referral.RewardAmount <= 0)
        {
            throw new BadRequestException("Số tiền thưởng bằng 0.");
        }

        var alreadyPaid = await _uow.ReferralRewardTransactions.Query()
            .AnyAsync(item => item.SellerReferralId == referral.Id && item.Status == "paid");
        if (alreadyPaid)
        {
            throw new BadRequestException("Thưởng này đã được chi.");
        }

        var referrerShop = await _uow.Shops.Query()
            .Where(shop => shop.OwnerUserId == referral.ReferrerUserId)
            .OrderBy(shop => shop.Id)
            .FirstOrDefaultAsync()
            ?? throw new BadRequestException("Người giới thiệu chưa có shop để nhận ví.");

        var wallet = await _uow.ShopWallets.Query()
            .FirstOrDefaultAsync(item => item.ShopId == referrerShop.Id);
        if (wallet == null)
        {
            wallet = new MiniLogistics.DAL.Models.ShopWallet
            {
                ShopId = referrerShop.Id,
                Balance = 0,
                UpdatedAt = DateTime.UtcNow
            };
            await _uow.ShopWallets.AddAsync(wallet);
            await _uow.SaveChangesAsync();
        }

        wallet.Balance += referral.RewardAmount;
        wallet.UpdatedAt = DateTime.UtcNow;
        _uow.ShopWallets.Update(wallet);

        var ledger = new MiniLogistics.DAL.Models.ShopWalletTransaction
        {
            WalletId = wallet.Id,
            Type = "REFERRAL",
            Amount = referral.RewardAmount,
            Description = $"Thưởng giới thiệu shop #{referral.ReferredShopId}",
            CreatedAt = DateTime.UtcNow
        };
        await _uow.ShopWalletTransactions.AddAsync(ledger);
        await _uow.SaveChangesAsync();

        var reward = await _uow.ReferralRewardTransactions.Query()
            .FirstOrDefaultAsync(item => item.SellerReferralId == referral.Id);
        if (reward == null)
        {
            reward = new ReferralRewardTransaction
            {
                SellerReferralId = referral.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.ReferralRewardTransactions.AddAsync(reward);
        }

        reward.Amount = referral.RewardAmount;
        reward.Status = "paid";
        reward.PaidAt = DateTime.UtcNow;
        reward.ShopWalletTransactionId = ledger.Id;

        referral.Status = "approved";
        referral.ReviewedByUserId = adminUserId;
        referral.UpdatedAt = DateTime.UtcNow;
    }

    private async Task CancelHoldAsync(long referralId)
    {
        var reward = await _uow.ReferralRewardTransactions.Query()
            .FirstOrDefaultAsync(item => item.SellerReferralId == referralId && item.Status == "held");
        if (reward != null)
        {
            reward.Status = "cancelled";
        }
    }

    private async Task<List<string>> DetectFraudAsync(long referrerId, long newUserId, string? ip, string? userAgent)
    {
        var flags = new List<string>();
        var referrer = await _uow.Users.GetByIdAsync(referrerId);
        var newbie = await _uow.Users.GetByIdAsync(newUserId);
        if (referrer != null && newbie != null)
        {
            if (!string.IsNullOrWhiteSpace(referrer.Phone) &&
                string.Equals(referrer.Phone, newbie.Phone, StringComparison.OrdinalIgnoreCase))
            {
                flags.Add("same_phone");
            }

            if (!string.IsNullOrWhiteSpace(referrer.FullName) &&
                string.Equals(referrer.FullName.Trim(), newbie.FullName?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                flags.Add("same_name");
            }
        }

        if (!string.IsNullOrWhiteSpace(ip))
        {
            var sameIp = await _uow.UserSessions.Query().AnyAsync(session =>
                session.UserId == referrerId && session.Ip == ip);
            if (sameIp)
            {
                flags.Add("same_ip");
            }
        }

        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            var sameDevice = await _uow.UserSessions.Query().AnyAsync(session =>
                session.UserId == referrerId && session.UserAgent == userAgent);
            if (sameDevice)
            {
                flags.Add("same_device");
            }
        }

        return flags;
    }

    private async Task<ReferralPolicy> EnsurePolicyAsync()
    {
        var policy = await _uow.ReferralPolicies.Query().OrderBy(item => item.Id).FirstOrDefaultAsync();
        if (policy != null)
        {
            return policy;
        }

        policy = new ReferralPolicy
        {
            RewardMode = "flat",
            FlatAmount = 50000,
            SharePercent = 20,
            PlatformFeePercent = 5,
            MinProductCount = 1,
            RequireApprovedShop = true,
            RequireFirstOrder = true,
            HoldingDays = 14,
            PayoutMode = "manual",
            IsEnabled = true,
            UpdatedAt = DateTime.UtcNow
        };
        await _uow.ReferralPolicies.AddAsync(policy);
        await _uow.SaveChangesAsync();
        return policy;
    }

    private async Task<string> NewCodeAsync()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var chars = new char[8];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            }

            var code = new string(chars);
            var taken = await _uow.Users.Query().AnyAsync(user => user.ReferralCode == code);
            if (!taken)
            {
                return code;
            }
        }

        throw new BadRequestException("Không tạo được mã giới thiệu. Hãy thử lại.");
    }

    private async Task<SellerReferral> LoadTrackedAsync(long id) =>
        await _uow.SellerReferrals.Query().FirstOrDefaultAsync(item => item.Id == id)
        ?? throw new NotFoundException("Không tìm thấy lượt giới thiệu.");

    private async Task<SellerReferralDTO> GetDtoAsync(long id)
    {
        var row = await _uow.SellerReferrals.Query()
            .AsNoTracking()
            .Include(item => item.ReferrerUser)
            .Include(item => item.ReferredShop)
            .Include(item => item.Rewards)
            .FirstAsync(item => item.Id == id);
        return MapReferral(row);
    }

    private static ReferralPolicyDTO MapPolicy(ReferralPolicy policy) => new()
    {
        RewardMode = policy.RewardMode,
        FlatAmount = policy.FlatAmount,
        SharePercent = policy.SharePercent,
        PlatformFeePercent = policy.PlatformFeePercent,
        MinProductCount = policy.MinProductCount,
        RequireApprovedShop = policy.RequireApprovedShop,
        RequireFirstOrder = policy.RequireFirstOrder,
        HoldingDays = policy.HoldingDays,
        PayoutMode = policy.PayoutMode,
        IsEnabled = policy.IsEnabled
    };

    private static SellerReferralDTO MapReferral(SellerReferral item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Status = item.Status,
        ReferrerName = item.ReferrerUser?.FullName ?? item.ReferrerUser?.Email,
        ReferredShopName = item.ReferredShop?.Name,
        FraudFlags = item.FraudFlags,
        RewardAmount = item.RewardAmount,
        QualifiedAt = item.QualifiedAt,
        PayableAt = item.PayableAt,
        ReviewNote = item.ReviewNote,
        CreatedAt = item.CreatedAt,
        RewardStatus = item.Rewards.OrderByDescending(reward => reward.Id).FirstOrDefault()?.Status
    };

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        return text.Length <= max ? text : text[..max];
    }
}
