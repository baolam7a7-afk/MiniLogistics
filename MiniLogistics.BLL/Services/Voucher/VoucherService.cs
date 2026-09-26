using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Voucher;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using VoucherModel =
    MiniLogistics.DAL.Models.Voucher;

namespace MiniLogistics.BLL.Services.Voucher;

public class VoucherService : IVoucherService
{
    private readonly IUnitOfWork _unitOfWork;

    public VoucherService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // =====================================================

    public async Task<PagedResponseDTO<VoucherResponseDTO>>
        GetAllAsync(
            VoucherPaginationRequestDTO request)
    {
        ValidatePagination(request);

        var vouchers =
            await _unitOfWork.Vouchers
                .GetAllAsync();

        var result =
            vouchers
                .Select(MapToResponseDTO)
                .ToList();

        // =================================================
        // SEARCH
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Search))
        {
            var search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            result =
                result
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.Code) &&
                         x.Code
                            .ToLowerInvariant()
                            .Contains(search))
                        ||
                        (!string.IsNullOrWhiteSpace(x.Name) &&
                         x.Name
                            .ToLowerInvariant()
                            .Contains(search)))
                    .ToList();
        }

        // =================================================
        // STATUS
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Status))
        {
            var status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            result =
                result
                    .Where(x =>
                        x.Status
                            .Equals(
                                status,
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        // =================================================
        // SCOPE
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Scope))
        {
            var scope =
                request.Scope
                    .Trim()
                    .ToLowerInvariant();

            result =
                result
                    .Where(x =>
                        x.Scope
                            .Equals(
                                scope,
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        // =================================================
        // SHOP
        // =================================================

        if (request.ShopId.HasValue)
        {
            result =
                result
                    .Where(x =>
                        x.ShopId ==
                        request.ShopId.Value)
                    .ToList();
        }

        // =================================================
        // SORT
        // =================================================

        result =
            result
                .OrderByDescending(x => x.Id)
                .ToList();

        // =================================================
        // PAGINATION
        // =================================================

        return CreatePagedResponse(
            result,
            request);
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<VoucherResponseDTO?>
        GetByIdAsync(
            long id)
    {
        var voucher =
            await _unitOfWork.Vouchers
                .GetByIdAsync(id);

        if (voucher == null)
        {
            return null;
        }

        return MapToResponseDTO(voucher);
    }


    // =====================================================
    // GET BY SHOP
    // =====================================================

    public async Task<PagedResponseDTO<VoucherResponseDTO>>
        GetByShopIdAsync(
            long shopId,
            VoucherPaginationRequestDTO request)
    {
        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }

        ValidatePagination(request);

        var vouchers =
            await _unitOfWork.Vouchers
                .FindAsync(
                    x =>
                        x.ShopId ==
                        shopId);

        var result =
            vouchers
                .Select(MapToResponseDTO)
                .ToList();

        // =================================================
        // SEARCH
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Search))
        {
            var search =
                request.Search
                    .Trim()
                    .ToLowerInvariant();

            result =
                result
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.Code) &&
                         x.Code
                            .ToLowerInvariant()
                            .Contains(search))
                        ||
                        (!string.IsNullOrWhiteSpace(x.Name) &&
                         x.Name
                            .ToLowerInvariant()
                            .Contains(search)))
                    .ToList();
        }

        // =================================================
        // STATUS
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Status))
        {
            var status =
                request.Status
                    .Trim()
                    .ToLowerInvariant();

            result =
                result
                    .Where(x =>
                        x.Status
                            .Equals(
                                status,
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        // =================================================
        // SORT
        // =================================================

        result =
            result
                .OrderByDescending(x => x.Id)
                .ToList();

        return CreatePagedResponse(
            result,
            request);
    }


    // =====================================================
    // GET BY CODE
    // =====================================================

    public async Task<VoucherResponseDTO?>
        GetByCodeAsync(
            string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode =
            code
                .Trim()
                .ToUpperInvariant();

        var vouchers =
            await _unitOfWork.Vouchers
                .FindAsync(
                    x =>
                        x.Code ==
                        normalizedCode);

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

    public async Task<VoucherResponseDTO>
        CreateAsync(
            long actorUserId,
            string actorRole,
            CreateVoucherDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (actorUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");
        }

        actorRole =
            actorRole
                .Trim()
                .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(
                request.Code))
        {
            throw new BadRequestException(
                "Voucher Code không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Scope))
        {
            throw new BadRequestException(
                "Scope không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(
                request.DiscountType))
        {
            throw new BadRequestException(
                "DiscountType không được để trống.");
        }

        var scope =
            request.Scope
                .Trim()
                .ToLowerInvariant();

        var discountType =
            request.DiscountType
                .Trim()
                .ToLowerInvariant();

        var code =
            request.Code
                .Trim()
                .ToUpperInvariant();

        // =================================================
        // 1. SCOPE
        // =================================================

        if (scope != "platform" &&
            scope != "shop")
        {
            throw new BadRequestException(
                "Scope phải là platform hoặc shop.");
        }

        // =================================================
        // 2. DISCOUNT TYPE
        // =================================================

        if (discountType != "percent" &&
            discountType != "amount")
        {
            throw new BadRequestException(
                "DiscountType phải là percent hoặc amount.");
        }

        // =================================================
        // 3. DISCOUNT VALUE
        // =================================================

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

        // =================================================
        // 4. TIME
        // =================================================

        if (request.EndAt <=
            request.StartAt)
        {
            throw new BadRequestException(
                "EndAt phải lớn hơn StartAt.");
        }

        // =================================================
        // 5. MIN ORDER
        // =================================================

        if (request.MinOrderValue < 0)
        {
            throw new BadRequestException(
                "MinOrderValue không được nhỏ hơn 0.");
        }

        // =================================================
        // 6. MAX DISCOUNT
        // =================================================

        if (request.MaxDiscount < 0)
        {
            throw new BadRequestException(
                "MaxDiscount không được nhỏ hơn 0.");
        }

        // =================================================
        // 7. SCOPE + SHOP
        // =================================================

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

        // =================================================
        // 8. DUPLICATE CODE
        // =================================================

        var existing =
            await _unitOfWork.Vouchers
                .FindAsync(
                    x =>
                        x.Code ==
                        code);

        if (existing.Any())
        {
            throw new BadRequestException(
                $"Voucher Code '{code}' đã tồn tại.");
        }

        // =================================================
        // 9. CREATE
        // =================================================

        var voucher =
            new VoucherModel
            {
                Scope =
                    scope,

                ShopId =
                    request.ShopId,

                Code =
                    code,

                Name =
                    request.Name?.Trim(),

                Description =
                    request.Description?.Trim(),

                DiscountType =
                    discountType,

                DiscountValue =
                    request.DiscountValue,

                MaxDiscount =
                    request.MaxDiscount,

                MinOrderValue =
                    request.MinOrderValue,

                UsageLimit =
                    request.UsageLimit,

                UsedCount =
                    0,

                StartAt =
                    request.StartAt,

                EndAt =
                    request.EndAt,

                Status =
                    "active",

                CreatedAt =
                    DateTime.UtcNow
            };

        await _unitOfWork.Vouchers
            .AddAsync(voucher);

        await _unitOfWork
            .SaveChangesAsync();

        return MapToResponseDTO(voucher);
    }


    // =====================================================
    // UPDATE
    // =====================================================

    public async Task<VoucherResponseDTO>
        UpdateAsync(
            long id,
            long actorUserId,
            string actorRole,
            UpdateVoucherDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        actorRole =
            actorRole
                .Trim()
                .ToLowerInvariant();

        var voucher =
            await _unitOfWork.Vouchers
                .GetByIdAsync(id);

        if (voucher == null)
        {
            throw new NotFoundException(
                $"Voucher {id} không tồn tại.");
        }

        // =================================================
        // CHECK ACCESS
        // =================================================

        await CheckVoucherAccessAsync(
            voucher,
            actorUserId,
            actorRole);

        // =================================================
        // SCOPE
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Scope))
        {
            var scope =
                request.Scope
                    .Trim()
                    .ToLowerInvariant();

            if (scope != "platform" &&
                scope != "shop")
            {
                throw new BadRequestException(
                    "Scope phải là platform hoặc shop.");
            }

            // Seller không được chuyển voucher
            // sang platform
            if (scope == "platform" &&
                actorRole != "admin")
            {
                throw new ForbiddenException(
                    "Seller không được quản lý Voucher platform.");
            }

            voucher.Scope =
                scope;
        }

        // =================================================
        // SHOP ID
        // =================================================

        if (request.ShopId.HasValue)
        {
            await CheckShopAccessAsync(
                request.ShopId.Value,
                actorUserId,
                actorRole);

            voucher.ShopId =
                request.ShopId.Value;
        }

        // =================================================
        // CODE
        // =================================================

        if (!string.IsNullOrWhiteSpace(
                request.Code))
        {
            var code =
                request.Code
                    .Trim()
                    .ToUpperInvariant();

            var duplicate =
                await _unitOfWork.Vouchers
                    .FindAsync(
                        x =>
                            x.Code == code &&
                            x.Id != id);

            if (duplicate.Any())
            {
                throw new BadRequestException(
                    $"Voucher Code '{code}' đã tồn tại.");
            }

            voucher.Code =
                code;
        }

        // =================================================
        // NAME
        // =================================================

        if (request.Name != null)
        {
            voucher.Name =
                request.Name.Trim();
        }

        // =================================================
        // DESCRIPTION
        // =================================================

        if (request.Description != null)
        {
            voucher.Description =
                request.Description.Trim();
        }

        // =================================================
        // DISCOUNT TYPE
        // =================================================

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

            voucher.DiscountType =
                type;
        }

        // =================================================
        // DISCOUNT VALUE
        // =================================================

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

        // =================================================
        // MAX DISCOUNT
        // =================================================

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

        // =================================================
        // MIN ORDER VALUE
        // =================================================

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

        // =================================================
        // USAGE LIMIT
        // =================================================

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

        // =================================================
        // START AT
        // =================================================

        if (request.StartAt.HasValue)
        {
            voucher.StartAt =
                request.StartAt.Value;
        }

        // =================================================
        // END AT
        // =================================================

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

        // =================================================
        // STATUS
        // =================================================

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

            voucher.Status =
                status;
        }

        _unitOfWork.Vouchers
            .Update(voucher);

        await _unitOfWork
            .SaveChangesAsync();

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
            actorRole
                .Trim()
                .ToLowerInvariant();

        var voucher =
            await _unitOfWork.Vouchers
                .GetByIdAsync(id);

        if (voucher == null)
        {
            throw new NotFoundException(
                $"Voucher {id} không tồn tại.");
        }

        await CheckVoucherAccessAsync(
            voucher,
            actorUserId,
            actorRole);

        // Không xóa voucher đã sử dụng
        if (voucher.UsedCount > 0)
        {
            throw new BadRequestException(
                "Không thể xóa Voucher đã được sử dụng.");
        }

        _unitOfWork.Vouchers
            .Delete(voucher);

        await _unitOfWork
            .SaveChangesAsync();
    }


    // =====================================================
    // VALIDATE
    // =====================================================

    public async Task<VoucherResponseDTO>
        ValidateAsync(
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
            code
                .Trim()
                .ToUpperInvariant();

        var vouchers =
            await _unitOfWork.Vouchers
                .FindAsync(
                    x =>
                        x.Code ==
                        normalizedCode);

        var voucher =
            vouchers.FirstOrDefault();

        if (voucher == null)
        {
            throw new NotFoundException(
                "Voucher không tồn tại.");
        }

        var now =
            DateTime.UtcNow;

        // =================================================
        // STATUS
        // =================================================

        if (voucher.Status != "active")
        {
            throw new BadRequestException(
                "Voucher không active.");
        }

        // =================================================
        // START
        // =================================================

        if (now < voucher.StartAt)
        {
            throw new BadRequestException(
                "Voucher chưa bắt đầu hiệu lực.");
        }

        // =================================================
        // END
        // =================================================

        if (now > voucher.EndAt)
        {
            throw new BadRequestException(
                "Voucher đã hết hạn.");
        }

        // =================================================
        // USAGE LIMIT
        // =================================================

        if (voucher.UsageLimit.HasValue &&
            voucher.UsedCount >=
            voucher.UsageLimit.Value)
        {
            throw new BadRequestException(
                "Voucher đã hết lượt sử dụng.");
        }

        // =================================================
        // MIN ORDER VALUE
        // =================================================

        if (voucher.MinOrderValue.HasValue &&
            orderValue <
            voucher.MinOrderValue.Value)
        {
            throw new BadRequestException(
                $"Đơn hàng tối thiểu là " +
                $"{voucher.MinOrderValue.Value:N0} VND.");
        }

        // =================================================
        // SHOP VOUCHER
        // =================================================

        if (voucher.Scope == "shop")
        {
            if (!shopId.HasValue)
            {
                throw new BadRequestException(
                    "Voucher shop cần ShopId.");
            }

            if (voucher.ShopId !=
                shopId.Value)
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
            actorRole
                .Trim()
                .ToLowerInvariant();

        var shops =
            await _unitOfWork.Shops
                .FindAsync(
                    x =>
                        x.Id ==
                        shopId);

        var shop =
            shops.FirstOrDefault();

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        // ADMIN
        if (actorRole == "admin")
        {
            return;
        }

        // SELLER
        if (actorRole == "seller" &&
            shop.OwnerUserId ==
            actorUserId)
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
            actorRole
                .Trim()
                .ToLowerInvariant();

        // ADMIN
        if (actorRole == "admin")
        {
            return;
        }

        // SELLER
        if (actorRole != "seller")
        {
            throw new ForbiddenException(
                "Role không được phép quản lý Voucher.");
        }

        // Seller chỉ được quản lý voucher shop
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

    private VoucherResponseDTO
        MapToResponseDTO(
            VoucherModel voucher)
    {
        return new VoucherResponseDTO
        {
            Id =
                voucher.Id,

            Scope =
                voucher.Scope,

            ShopId =
                voucher.ShopId,

            Code =
                voucher.Code,

            Name =
                voucher.Name,

            Description =
                voucher.Description,

            DiscountType =
                voucher.DiscountType,

            DiscountValue =
                voucher.DiscountValue,

            MaxDiscount =
                voucher.MaxDiscount,

            MinOrderValue =
                voucher.MinOrderValue,

            UsageLimit =
                voucher.UsageLimit,

            UsedCount =
                voucher.UsedCount,

            StartAt =
                voucher.StartAt,

            EndAt =
                voucher.EndAt,

            Status =
                voucher.Status,

            CreatedAt =
                voucher.CreatedAt
        };
    }


    // =====================================================
    // PAGINATION
    // =====================================================

    private void ValidatePagination(
        VoucherPaginationRequestDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (request.Page < 1)
        {
            throw new BadRequestException(
                "Page phải lớn hơn hoặc bằng 1.");
        }

        if (request.PageSize < 1)
        {
            throw new BadRequestException(
                "PageSize phải lớn hơn hoặc bằng 1.");
        }

        if (request.PageSize > 100)
        {
            throw new BadRequestException(
                "PageSize không được lớn hơn 100.");
        }
    }


    // =====================================================
    // CREATE PAGED RESPONSE
    // =====================================================

    private PagedResponseDTO<VoucherResponseDTO>
        CreatePagedResponse(
            List<VoucherResponseDTO> items,
            VoucherPaginationRequestDTO request)
    {
        var totalItems =
            items.Count;

        var totalPages =
            (int)Math.Ceiling(
                totalItems /
                (double)request.PageSize);

        var pagedItems =
            items
                .Skip(
                    (request.Page - 1) *
                    request.PageSize)
                .Take(
                    request.PageSize)
                .ToList();

        return new PagedResponseDTO<VoucherResponseDTO>
        {
            Items =
                pagedItems,

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
}