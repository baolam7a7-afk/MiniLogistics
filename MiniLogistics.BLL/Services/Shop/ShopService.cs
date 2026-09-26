using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Shop;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using ShopModel = MiniLogistics.DAL.Models.Shop;

namespace MiniLogistics.BLL.Services.Shop;

public class ShopService : IShopService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShopService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // CREATE SHOP
    // SELLER ONLY
    // =====================================================

    public async Task<ShopResponseDTO> CreateAsync(
        long ownerUserId,
        CreateShopDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "Tên Shop không được để trống.");
        }

        var name = request.Name.Trim();

        if (name.Length > 200)
        {
            throw new BadRequestException(
                "Tên Shop không được vượt quá 200 ký tự.");
        }


        // =================================================
        // CHECK USER
        // =================================================

        var owner =
            await _unitOfWork.Users
                .GetByIdAsync(ownerUserId);

        if (owner == null)
        {
            throw new NotFoundException(
                "Không tìm thấy User.");
        }


        // =================================================
        // GENERATE SLUG
        // =================================================

        var baseSlug =
            GenerateSlug(name);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            throw new BadRequestException(
                "Tên Shop không thể tạo Slug hợp lệ.");
        }

        var slug =
            await GenerateUniqueSlugAsync(
                baseSlug);


        // =================================================
        // CREATE ENTITY
        // =================================================

        var shop =
            new ShopModel
            {
                OwnerUserId =
                    ownerUserId,

                Name =
                    name,

                Slug =
                    slug,

                Description =
                    string.IsNullOrWhiteSpace(
                        request.Description)
                        ? null
                        : request.Description.Trim(),

                LogoUrl =
                    string.IsNullOrWhiteSpace(
                        request.LogoUrl)
                        ? null
                        : request.LogoUrl.Trim(),

                // Shop mới phải chờ Admin duyệt
                Status =
                    "pending",

                ApprovedAt =
                    null,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    null
            };


        await _unitOfWork.Shops
            .AddAsync(shop);

        await _unitOfWork
            .SaveChangesAsync();


        return MapToResponseDTO(shop);
    }


    // =====================================================
    // GET MY SHOPS
    // SELLER ONLY
    // =====================================================

    public async Task<IEnumerable<ShopResponseDTO>>
        GetMyShopsAsync(
            long ownerUserId)
    {
        var shops =
            await _unitOfWork.Shops
                .FindAsync(x =>
                    x.OwnerUserId == ownerUserId);

        return shops
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapToResponseDTO)
            .ToList();
    }


    // =====================================================
    // GET SHOP BY ID
    // COMMON
    // =====================================================

    public async Task<ShopResponseDTO?>
        GetByIdAsync(
            long shopId)
    {
        var shop =
            await _unitOfWork.Shops
                .GetByIdAsync(shopId);

        if (shop == null)
        {
            return null;
        }

        return MapToResponseDTO(shop);
    }


    // =====================================================
    // GET MY SHOP BY ID
    // SELLER ONLY
    // =====================================================

    public async Task<ShopResponseDTO?>
        GetMyShopByIdAsync(
            long ownerUserId,
            long shopId)
    {
        var shops =
            await _unitOfWork.Shops
                .FindAsync(x =>
                    x.Id == shopId &&
                    x.OwnerUserId == ownerUserId);

        var entity =
            shops.FirstOrDefault();

        if (entity == null)
        {
            return null;
        }

        return MapToResponseDTO(entity);
    }


    // =====================================================
    // UPDATE SHOP
    // SELLER ONLY
    // =====================================================

    public async Task<ShopResponseDTO?>
        UpdateAsync(
            long ownerUserId,
            long shopId,
            UpdateShopDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "Tên Shop không được để trống.");
        }


        var shops =
            await _unitOfWork.Shops
                .FindAsync(x =>
                    x.Id == shopId &&
                    x.OwnerUserId == ownerUserId);

        var entity =
            shops.FirstOrDefault();

        if (entity == null)
        {
            return null;
        }


        // =================================================
        // UPDATE NAME
        // =================================================

        var name =
            request.Name.Trim();

        if (name.Length > 200)
        {
            throw new BadRequestException(
                "Tên Shop không được vượt quá 200 ký tự.");
        }


        // =================================================
        // UPDATE SLUG
        // =================================================

        if (!string.Equals(
                entity.Name,
                name,
                StringComparison.OrdinalIgnoreCase))
        {
            var baseSlug =
                GenerateSlug(name);

            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                throw new BadRequestException(
                    "Tên Shop không thể tạo Slug hợp lệ.");
            }

            entity.Slug =
                await GenerateUniqueSlugAsync(
                    baseSlug,
                    entity.Id);
        }


        entity.Name =
            name;

        entity.Description =
            string.IsNullOrWhiteSpace(
                request.Description)
                ? null
                : request.Description.Trim();

        entity.LogoUrl =
            string.IsNullOrWhiteSpace(
                request.LogoUrl)
                ? null
                : request.LogoUrl.Trim();

        entity.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Shops
            .Update(entity);

        await _unitOfWork
            .SaveChangesAsync();


        return MapToResponseDTO(entity);
    }


    // =====================================================
    // ADMIN - GET ALL SHOPS
    // PAGINATION + SEARCH + STATUS
    // =====================================================

    public async Task<PagedResponseDTO<ShopResponseDTO>>
        GetAllAsync(
            ShopPaginationRequestDTO request)
    {
        if (request == null)
        {
            request =
                new ShopPaginationRequestDTO();
        }


        // =================================================
        // VALIDATE PAGINATION
        // =================================================

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


        var search =
            request.Search?
                .Trim()
                .ToLower();

        var status =
            request.Status?
                .Trim()
                .ToLower();


        // =================================================
        // BUILD FILTER
        // =================================================

        Expression<Func<ShopModel, bool>>? predicate = null;


        // -------------------------------------------------
        // SEARCH + STATUS
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(search) &&
            !string.IsNullOrWhiteSpace(status))
        {
            predicate =
                shop =>
                    (
                        shop.Name
                            .ToLower()
                            .Contains(search) ||

                        shop.Slug
                            .ToLower()
                            .Contains(search)
                    )
                    &&
                    shop.Status
                        .ToLower()
                        .Equals(status);
        }


        // -------------------------------------------------
        // SEARCH ONLY
        // -------------------------------------------------

        else if (!string.IsNullOrWhiteSpace(search))
        {
            predicate =
                shop =>
                    shop.Name
                        .ToLower()
                        .Contains(search) ||

                    shop.Slug
                        .ToLower()
                        .Contains(search);
        }


        // -------------------------------------------------
        // STATUS ONLY
        // -------------------------------------------------

        else if (!string.IsNullOrWhiteSpace(status))
        {
            predicate =
                shop =>
                    shop.Status
                        .ToLower()
                        .Equals(status);
        }


        // =================================================
        // QUERY DATABASE
        // =================================================

        var result =
            await _unitOfWork.Shops
                .GetPagedAsync(
                    request.Page,
                    request.PageSize,
                    predicate,
                    query =>
                        query.OrderByDescending(
                            x => x.Id));


        // =================================================
        // MAP DATA
        // =================================================

        var items =
            result.Items
                .Select(MapToResponseDTO)
                .ToList();


        // =================================================
        // CALCULATE TOTAL PAGES
        // =================================================

        var totalPages =
            result.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    result.TotalItems /
                    (double)request.PageSize);


        // =================================================
        // RETURN
        // =================================================

        return new PagedResponseDTO<ShopResponseDTO>
        {
            Items =
                items,

            Page =
                request.Page,

            PageSize =
                request.PageSize,

            TotalItems =
                result.TotalItems,

            TotalPages =
                totalPages
        };
    }


    // =====================================================
    // ADMIN - GET PENDING SHOPS
    // =====================================================

    public async Task<IEnumerable<ShopResponseDTO>>
        GetPendingAsync()
    {
        var shops =
            await _unitOfWork.Shops
                .FindAsync(x =>
                    x.Status == "pending");

        return shops
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapToResponseDTO)
            .ToList();
    }


    // =====================================================
    // ADMIN - APPROVE SHOP
    // =====================================================

    public async Task<ShopResponseDTO?>
        ApproveAsync(
            long shopId)
    {
        // -------------------------------------------------
        // VALIDATE SHOP ID
        // -------------------------------------------------

        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }


        // -------------------------------------------------
        // GET SHOP
        // -------------------------------------------------

        var shop =
            await _unitOfWork.Shops
                .GetByIdAsync(shopId);

        if (shop == null)
        {
            return null;
        }


        // -------------------------------------------------
        // ONLY PENDING CAN BE APPROVED
        // -------------------------------------------------

        if (shop.Status != "pending")
        {
            throw new BadRequestException(
                $"Shop {shopId} không ở trạng thái pending.");
        }


        // -------------------------------------------------
        // APPROVE
        // -------------------------------------------------

        shop.Status =
            "approved";

        shop.ApprovedAt =
            DateTime.UtcNow;

        shop.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Shops
            .Update(shop);

        await _unitOfWork
            .SaveChangesAsync();


        return MapToResponseDTO(shop);
    }


    // =====================================================
    // ADMIN - REJECT SHOP
    // =====================================================

    public async Task<ShopResponseDTO?>
        RejectAsync(
            long shopId)
    {
        // -------------------------------------------------
        // VALIDATE SHOP ID
        // -------------------------------------------------

        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }


        // -------------------------------------------------
        // GET SHOP
        // -------------------------------------------------

        var shop =
            await _unitOfWork.Shops
                .GetByIdAsync(shopId);

        if (shop == null)
        {
            return null;
        }


        // -------------------------------------------------
        // ONLY PENDING CAN BE REJECTED
        // -------------------------------------------------

        if (shop.Status != "pending")
        {
            throw new BadRequestException(
                $"Shop {shopId} không ở trạng thái pending.");
        }


        // -------------------------------------------------
        // REJECT
        // -------------------------------------------------

        shop.Status =
            "rejected";

        shop.ApprovedAt =
            null;

        shop.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Shops
            .Update(shop);

        await _unitOfWork
            .SaveChangesAsync();


        return MapToResponseDTO(shop);
    }


    // =====================================================
    // GENERATE UNIQUE SLUG
    // =====================================================

    private async Task<string>
        GenerateUniqueSlugAsync(
            string baseSlug,
            long? ignoreShopId = null)
    {
        var slug =
            baseSlug;

        var counter =
            2;


        while (true)
        {
            var existing =
                await _unitOfWork.Shops
                    .FindAsync(x =>
                        x.Slug == slug &&
                        (
                            !ignoreShopId.HasValue ||
                            x.Id != ignoreShopId.Value
                        ));

            if (!existing.Any())
            {
                return slug;
            }

            slug =
                $"{baseSlug}-{counter}";

            counter++;
        }
    }


    // =====================================================
    // GENERATE SLUG
    // =====================================================

    private static string GenerateSlug(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text =
            text.Trim()
                .ToLowerInvariant();

        text =
            RemoveVietnameseCharacters(text);

        text =
            Regex.Replace(
                text,
                @"[^a-z0-9\s-]",
                "");

        text =
            Regex.Replace(
                text,
                @"\s+",
                "-");

        text =
            Regex.Replace(
                text,
                @"-+",
                "-");

        return text.Trim('-');
    }


    // =====================================================
    // REMOVE VIETNAMESE CHARACTERS
    // =====================================================

    private static string
        RemoveVietnameseCharacters(
            string text)
    {
        var normalized =
            text.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in normalized)
        {
            var unicodeCategory =
                System.Globalization
                    .CharUnicodeInfo
                    .GetUnicodeCategory(
                        character);

            if (unicodeCategory !=
                System.Globalization
                    .UnicodeCategory
                    .NonSpacingMark)
            {
                builder.Append(
                    character);
            }
        }

        var result =
            builder
                .ToString()
                .Normalize(
                    NormalizationForm.FormC);

        result =
            result
                .Replace(
                    "đ",
                    "d")
                .Replace(
                    "Đ",
                    "d");

        return result;
    }


    // =====================================================
    // MAP
    // =====================================================

    private static ShopResponseDTO
        MapToResponseDTO(
            ShopModel shop)
    {
        return new ShopResponseDTO
        {
            Id =
                shop.Id,

            OwnerUserId =
                shop.OwnerUserId,

            Name =
                shop.Name,

            Slug =
                shop.Slug,

            Description =
                shop.Description,

            LogoUrl =
                shop.LogoUrl,

            Status =
                shop.Status,

            ApprovedAt =
                shop.ApprovedAt,

            CreatedAt =
                shop.CreatedAt,

            UpdatedAt =
                shop.UpdatedAt
        };
    }
}