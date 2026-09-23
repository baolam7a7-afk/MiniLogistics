using MiniLogistics.BLL.DTOs.Voucher;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using VoucherModel = MiniLogistics.DAL.Models.Voucher;

namespace MiniLogistics.BLL.Services.Voucher;

public class VoucherService : IVoucherService
{
    private readonly IUnitOfWork _unitOfWork;

    public VoucherService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // GET ALL
    // =====================================================

    public async Task<IEnumerable<VoucherResponseDTO>> GetAllAsync()
    {
        var vouchers =
            await _unitOfWork.Vouchers.GetAllAsync();

        return vouchers
            .OrderByDescending(x => x.Id)
            .Select(MapToResponseDTO)
            .ToList();
    }

    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<VoucherResponseDTO?> GetByIdAsync(long id)
    {
        var voucher =
            await _unitOfWork.Vouchers.GetByIdAsync(id);

        if (voucher == null)
        {
            return null;
        }

        return MapToResponseDTO(voucher);
    }

    // =====================================================
    // GET BY SHOP
    // =====================================================

    public async Task<IEnumerable<VoucherResponseDTO>>
        GetByShopIdAsync(long shopId)
    {
        var vouchers =
            await _unitOfWork.Vouchers.FindAsync(
                x => x.ShopId == shopId);

        return vouchers
            .OrderByDescending(x => x.Id)
            .Select(MapToResponseDTO)
            .ToList();
    }

    // =====================================================
    // GET BY CODE
    // =====================================================

    public async Task<VoucherResponseDTO?> GetByCodeAsync(
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        var vouchers =
            await _unitOfWork.Vouchers.FindAsync(
                x => x.Code == normalizedCode);

        var voucher =
            vouchers.FirstOrDefault();

        if (voucher == null)
        {
            return null;
        }

        return MapToResponseDTO(voucher);
    }

    // =====================================================
    // CREATE
    // =====================================================

    public async Task<VoucherResponseDTO> CreateAsync(
        long actorUserId,
        string actorRole,
        CreateVoucherDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        actorRole =
            actorRole.Trim().ToLowerInvariant();

        var scope =
            request.Scope.Trim().ToLowerInvariant();

        var discountType =
            request.DiscountType.Trim().ToLowerInvariant();

        var code =
            request.Code.Trim().ToUpperInvariant();

        // -------------------------------------------------
        // 1. Validate Scope
        // -------------------------------------------------

        if (scope != "platform" &&
            scope != "shop")
        {
            throw new BadRequestException(
                "Scope phải là platform hoặc shop.");
        }

        // -------------------------------------------------
        // 2. Validate DiscountType
        // -------------------------------------------------

        if (discountType != "percent" &&
            discountType != "amount")
        {
            throw new BadRequestException(
                "DiscountType phải là percent hoặc amount.");
        }

        // -------------------------------------------------
        // 3. Validate DiscountValue
        // -------------------------------------------------

        if (request.DiscountValue <= 0)
        {
            throw new BadRequestException(
                "DiscountValue phải lớn hơn 0.");
        }

        if (discountType == "percent" &&
            request.DiscountValue > 100)
        {
            throw new BadRequestException(
                "Voucher percent không được vượt quá 100.");
        }

        // -------------------------------------------------
        // 4. Validate thời gian
        // -------------------------------------------------

        if (request.EndAt <= request.StartAt)
        {
            throw new BadRequestException(
                "EndAt phải lớn hơn StartAt.");
        }

        // -------------------------------------------------
        // 5. Validate MinOrderValue
        // -------------------------------------------------

        if (request.MinOrderValue < 0)
        {
            throw new BadRequestException(
                "MinOrderValue không được nhỏ hơn 0.");
        }

        // -------------------------------------------------
        // 6. Validate MaxDiscount
        // -------------------------------------------------

        if (request.MaxDiscount < 0)
        {
            throw new BadRequestException(
                "MaxDiscount không được nhỏ hơn 0.");
        }

        // -------------------------------------------------
        // 7. Validate Scope + Shop
        // -------------------------------------------------

        if (scope == "platform")
        {
            if (request.ShopId.HasValue)
            {
                throw new BadRequestException(
                    "Voucher platform không được có ShopId.");
            }

            if (actorRole != "admin")
            {
                throw new ForbiddenException(
                    "Chỉ admin được tạo Voucher platform.");
            }
        }

        if (scope == "shop")
        {
            if (!request.ShopId.HasValue)
            {
                throw new BadRequestException(
                    "Voucher shop bắt buộc phải có ShopId.");
            }

            if (actorRole != "admin" &&
                actorRole != "seller")
            {
                throw new ForbiddenException(
                    "Role không được phép tạo Voucher shop.");
            }

            await CheckShopAccessAsync(
                request.ShopId.Value,
                actorUserId,
                actorRole);
        }

        // -------------------------------------------------
        // 8. Kiểm tra Code trùng
        // -------------------------------------------------

        var existing =
            await _unitOfWork.Vouchers.FindAsync(
                x => x.Code == code);

        if (existing.Any())
        {
            throw new BadRequestException(
                $"Voucher Code '{code}' đã tồn tại.");
        }

        // -------------------------------------------------
        // 9. Create
        // -------------------------------------------------

        var voucher = new VoucherModel
        {
            Scope = scope,
            ShopId = request.ShopId,
            Code = code,
            Name = request.Name?.Trim(),
            Description = request.Description?.Trim(),
            DiscountType = discountType,
            DiscountValue = request.DiscountValue,
            MaxDiscount = request.MaxDiscount,
            MinOrderValue = request.MinOrderValue,
            UsageLimit = request.UsageLimit,
            UsedCount = 0,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.Vouchers.AddAsync(voucher);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponseDTO(voucher);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    public async Task<VoucherResponseDTO> UpdateAsync(
        long id,
        long actorUserId,
        string actorRole,
        UpdateVoucherDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        actorRole =
            actorRole.Trim().ToLowerInvariant();

        var voucher =
            await _unitOfWork.Vouchers.GetByIdAsync(id);

        if (voucher == null)
        {
            throw new NotFoundException(
                $"Voucher {id} không tồn tại.");
        }

        // -------------------------------------------------
        // 1. Check quyền
        // -------------------------------------------------

        await CheckVoucherAccessAsync(
            voucher,
            actorUserId,
            actorRole);

        // -------------------------------------------------
        // 2. Scope
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(request.Scope))
        {
            var scope =
                request.Scope.Trim().ToLowerInvariant();

            if (scope != "platform" &&
                scope != "shop")
            {
                throw new BadRequestException(
                    "Scope phải là platform hoặc shop.");
            }

            voucher.Scope = scope;
        }

        // -------------------------------------------------
        // 3. ShopId
        // -------------------------------------------------

        if (request.ShopId.HasValue)
        {
            await CheckShopAccessAsync(
                request.ShopId.Value,
                actorUserId,
                actorRole);

            voucher.ShopId =
                request.ShopId.Value;
        }

        // -------------------------------------------------
        // 4. Code
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code =
                request.Code.Trim().ToUpperInvariant();

            var duplicate =
                await _unitOfWork.Vouchers.FindAsync(
                    x =>
                        x.Code == code
                        && x.Id != id);

            if (duplicate.Any())
            {
                throw new BadRequestException(
                    $"Voucher Code '{code}' đã tồn tại.");
            }

            voucher.Code = code;
        }

        // -------------------------------------------------
        // 5. Name
        // -------------------------------------------------

        if (request.Name != null)
        {
            voucher.Name =
                request.Name.Trim();
        }

        // -------------------------------------------------
        // 6. Description
        // -------------------------------------------------

        if (request.Description != null)
        {
            voucher.Description =
                request.Description.Trim();
        }

        // -------------------------------------------------
        // 7. DiscountType
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            request.DiscountType))
        {
            var type =
                request.DiscountType
                    .Trim()
                    .ToLowerInvariant();

            if (type != "percent" &&
                type != "amount")
            {
                throw new BadRequestException(
                    "DiscountType phải là percent hoặc amount.");
            }

            voucher.DiscountType = type;
        }

        // -------------------------------------------------
        // 8. DiscountValue
        // -------------------------------------------------

        if (request.DiscountValue.HasValue)
        {
            if (request.DiscountValue.Value <= 0)
            {
                throw new BadRequestException(
                    "DiscountValue phải lớn hơn 0.");
            }

            if (voucher.DiscountType == "percent" &&
                request.DiscountValue.Value > 100)
            {
                throw new BadRequestException(
                    "Voucher percent không được vượt quá 100.");
            }

            voucher.DiscountValue =
                request.DiscountValue.Value;
        }

        // -------------------------------------------------
        // 9. MaxDiscount
        // -------------------------------------------------

        if (request.MaxDiscount.HasValue)
        {
            if (request.MaxDiscount.Value < 0)
            {
                throw new BadRequestException(
                    "MaxDiscount không được nhỏ hơn 0.");
            }

            voucher.MaxDiscount =
                request.MaxDiscount.Value;
        }

        // -------------------------------------------------
        // 10. MinOrderValue
        // -------------------------------------------------

        if (request.MinOrderValue.HasValue)
        {
            if (request.MinOrderValue.Value < 0)
            {
                throw new BadRequestException(
                    "MinOrderValue không được nhỏ hơn 0.");
            }

            voucher.MinOrderValue =
                request.MinOrderValue.Value;
        }

        // -------------------------------------------------
        // 11. UsageLimit
        // -------------------------------------------------

        if (request.UsageLimit.HasValue)
        {
            if (request.UsageLimit.Value <= 0)
            {
                throw new BadRequestException(
                    "UsageLimit phải lớn hơn 0.");
            }

            if (request.UsageLimit.Value <
                voucher.UsedCount)
            {
                throw new BadRequestException(
                    "UsageLimit không được nhỏ hơn UsedCount.");
            }

            voucher.UsageLimit =
                request.UsageLimit.Value;
        }

        // -------------------------------------------------
        // 12. StartAt
        // -------------------------------------------------

        if (request.StartAt.HasValue)
        {
            voucher.StartAt =
                request.StartAt.Value;
        }

        // -------------------------------------------------
        // 13. EndAt
        // -------------------------------------------------

        if (request.EndAt.HasValue)
        {
            voucher.EndAt =
                request.EndAt.Value;
        }

        if (voucher.EndAt <=
            voucher.StartAt)
        {
            throw new BadRequestException(
                "EndAt phải lớn hơn StartAt.");
        }

        // -------------------------------------------------
        // 14. Status
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            request.Status))
        {
            var status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            if (status != "active" &&
                status != "inactive" &&
                status != "expired")
            {
                throw new BadRequestException(
                    "Status không hợp lệ.");
            }

            voucher.Status = status;
        }

        _unitOfWork.Vouchers.Update(voucher);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponseDTO(voucher);
    }

    // =====================================================
    // DELETE
    // =====================================================

    public async Task DeleteAsync(
        long id,
        long actorUserId,
        string actorRole)
    {
        actorRole =
            actorRole.Trim().ToLowerInvariant();

        var voucher =
            await _unitOfWork.Vouchers.GetByIdAsync(id);

        if (voucher == null)
        {
            throw new NotFoundException(
                $"Voucher {id} không tồn tại.");
        }

        await CheckVoucherAccessAsync(
            voucher,
            actorUserId,
            actorRole);

        // Không xóa voucher đã được sử dụng.
        if (voucher.UsedCount > 0)
        {
            throw new BadRequestException(
                "Không thể xóa Voucher đã được sử dụng.");
        }

        _unitOfWork.Vouchers.Delete(voucher);

        await _unitOfWork.SaveChangesAsync();
    }

    // =====================================================
    // VALIDATE
    // =====================================================

    public async Task<VoucherResponseDTO> ValidateAsync(
        string code,
        decimal orderValue,
        long? shopId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new BadRequestException(
                "Code không được để trống.");
        }

        if (orderValue < 0)
        {
            throw new BadRequestException(
                "OrderValue không được nhỏ hơn 0.");
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        var vouchers =
            await _unitOfWork.Vouchers.FindAsync(
                x => x.Code == normalizedCode);

        var voucher =
            vouchers.FirstOrDefault();

        if (voucher == null)
        {
            throw new NotFoundException(
                "Voucher không tồn tại.");
        }

        var now = DateTime.UtcNow;

        // -------------------------------------------------
        // 1. Status
        // -------------------------------------------------

        if (voucher.Status != "active")
        {
            throw new BadRequestException(
                "Voucher không active.");
        }

        // -------------------------------------------------
        // 2. StartAt
        // -------------------------------------------------

        if (now < voucher.StartAt)
        {
            throw new BadRequestException(
                "Voucher chưa bắt đầu hiệu lực.");
        }

        // -------------------------------------------------
        // 3. EndAt
        // -------------------------------------------------

        if (now > voucher.EndAt)
        {
            throw new BadRequestException(
                "Voucher đã hết hạn.");
        }

        // -------------------------------------------------
        // 4. UsageLimit
        // -------------------------------------------------

        if (voucher.UsageLimit.HasValue &&
            voucher.UsedCount >=
            voucher.UsageLimit.Value)
        {
            throw new BadRequestException(
                "Voucher đã hết lượt sử dụng.");
        }

        // -------------------------------------------------
        // 5. MinOrderValue
        // -------------------------------------------------

        if (voucher.MinOrderValue.HasValue &&
            orderValue < voucher.MinOrderValue.Value)
        {
            throw new BadRequestException(
                $"Đơn hàng tối thiểu là {voucher.MinOrderValue.Value:N0} VND.");
        }

        // -------------------------------------------------
        // 6. Shop Voucher
        // -------------------------------------------------

        if (voucher.Scope == "shop")
        {
            if (!shopId.HasValue)
            {
                throw new BadRequestException(
                    "Voucher shop cần ShopId.");
            }

            if (voucher.ShopId != shopId.Value)
            {
                throw new BadRequestException(
                    "Voucher không áp dụng cho Shop này.");
            }
        }

        return MapToResponseDTO(voucher);
    }

    // =====================================================
    // CHECK SHOP ACCESS
    // =====================================================

    private async Task CheckShopAccessAsync(
        long shopId,
        long actorUserId,
        string actorRole)
    {
        actorRole =
            actorRole.Trim().ToLowerInvariant();

        var shops =
            await _unitOfWork.Shops.FindAsync(
                x =>
                    x.Id == shopId);

        var shop =
            shops.FirstOrDefault();

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        if (actorRole == "admin")
        {
            return;
        }

        if (actorRole == "seller" &&
            shop.OwnerUserId == actorUserId)
        {
            return;
        }

        throw new ForbiddenException(
            "Bạn không có quyền quản lý Voucher của Shop này.");
    }

    // =====================================================
    // CHECK VOUCHER ACCESS
    // =====================================================

    private async Task CheckVoucherAccessAsync(
        VoucherModel voucher,
        long actorUserId,
        string actorRole)
    {
        actorRole =
            actorRole.Trim().ToLowerInvariant();

        if (actorRole == "admin")
        {
            return;
        }

        if (actorRole != "seller")
        {
            throw new ForbiddenException(
                "Role không được phép quản lý Voucher.");
        }

        if (voucher.Scope != "shop" ||
            !voucher.ShopId.HasValue)
        {
            throw new ForbiddenException(
                "Seller không được quản lý Voucher platform.");
        }

        await CheckShopAccessAsync(
            voucher.ShopId.Value,
            actorUserId,
            actorRole);
    }

    // =====================================================
    // MAP
    // =====================================================

    private VoucherResponseDTO MapToResponseDTO(
        VoucherModel voucher)
    {
        return new VoucherResponseDTO
        {
            Id = voucher.Id,
            Scope = voucher.Scope,
            ShopId = voucher.ShopId,
            Code = voucher.Code,
            Name = voucher.Name,
            Description = voucher.Description,
            DiscountType = voucher.DiscountType,
            DiscountValue = voucher.DiscountValue,
            MaxDiscount = voucher.MaxDiscount,
            MinOrderValue = voucher.MinOrderValue,
            UsageLimit = voucher.UsageLimit,
            UsedCount = voucher.UsedCount,
            StartAt = voucher.StartAt,
            EndAt = voucher.EndAt,
            Status = voucher.Status,
            CreatedAt = voucher.CreatedAt
        };
    }
}