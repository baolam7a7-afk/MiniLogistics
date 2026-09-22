using MiniLogistics.BLL.DTOs.Shop;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;
using ShopEntity = MiniLogistics.DAL.Models.Shop;

namespace MiniLogistics.BLL.Services.Shop;

public class ShopService : IShopService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShopService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // CREATE SHOP
    // =====================================================

    public async Task<ShopResponseDto> CreateShopAsync(
        long userId,
        CreateShopDto request)
    {
        // -------------------------------------------------
        // 1. Validate Name
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "Tên Shop không được để trống."
            );
        }

        // -------------------------------------------------
        // 2. Validate Slug
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống."
            );
        }

        // -------------------------------------------------
        // 3. Kiểm tra User tồn tại
        // -------------------------------------------------

        var user = await _unitOfWork.Users
            .GetByIdAsync(userId);

        if (user == null)
        {
            throw new NotFoundException(
                "User không tồn tại."
            );
        }

        // -------------------------------------------------
        // 4. Kiểm tra User đã có Shop chưa
        // -------------------------------------------------

        var existingShops = await _unitOfWork.Shops
            .FindAsync(x => x.OwnerUserId == userId);

        if (existingShops.Any())
        {
            throw new BadRequestException(
                "User này đã có Shop."
            );
        }

        // -------------------------------------------------
        // 5. Kiểm tra Slug đã tồn tại chưa
        // -------------------------------------------------

        var existingSlug = await _unitOfWork.Shops
            .FindAsync(x => x.Slug == request.Slug.Trim().ToLower());

        if (existingSlug.Any())
        {
            throw new BadRequestException(
                "Slug Shop đã tồn tại."
            );
        }

        // -------------------------------------------------
        // 6. Tạo Shop
        // -------------------------------------------------

        var shop = new ShopEntity
        {
            OwnerUserId = userId,

            Name = request.Name.Trim(),

            Slug = request.Slug.Trim().ToLower(),

            Description = request.Description,

            LogoUrl = request.LogoUrl,

            Status = "pending",

            ApprovedAt = null,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = null
        };

        // -------------------------------------------------
        // 7. Add Shop vào Database
        // -------------------------------------------------

        await _unitOfWork.Shops.AddAsync(shop);

        // -------------------------------------------------
        // 8. Save Database
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();

        // -------------------------------------------------
        // 9. Convert Entity -> DTO
        // -------------------------------------------------

        return MapToDto(shop);
    }


    // =====================================================
    // GET MY SHOP
    // =====================================================

    public async Task<ShopResponseDto> GetMyShopAsync(
        long userId)
    {
        // -------------------------------------------------
        // 1. Tìm Shop theo OwnerUserId
        // -------------------------------------------------

        var shops = await _unitOfWork.Shops
            .FindAsync(x => x.OwnerUserId == userId);

        var shop = shops.FirstOrDefault();

        // -------------------------------------------------
        // 2. Không tìm thấy Shop
        // -------------------------------------------------

        if (shop == null)
        {
            throw new NotFoundException(
                "Bạn chưa có Shop."
            );
        }

        // -------------------------------------------------
        // 3. Convert Entity -> DTO
        // -------------------------------------------------

        return MapToDto(shop);
    }


    // =====================================================
    // MAPPING
    // =====================================================

    private static ShopResponseDto MapToDto(
        ShopEntity shop)
    {
        return new ShopResponseDto
        {
            Id = shop.Id,

            OwnerUserId = shop.OwnerUserId,

            Name = shop.Name,

            Slug = shop.Slug,

            Description = shop.Description,

            LogoUrl = shop.LogoUrl,

            Status = shop.Status,

            ApprovedAt = shop.ApprovedAt,

            CreatedAt = shop.CreatedAt,

            UpdatedAt = shop.UpdatedAt
        };
    }
}