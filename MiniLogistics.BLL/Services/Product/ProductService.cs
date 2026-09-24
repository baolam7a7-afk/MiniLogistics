using MiniLogistics.BLL.DTOs.Product;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using ProductEntity = MiniLogistics.DAL.Models.Product;

namespace MiniLogistics.BLL.Services.Product;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // PUBLIC
    // =====================================================

    public async Task<IEnumerable<ProductResponseDTO>> GetAllAsync()
    {
        var products =
            await _unitOfWork.Products.GetAllAsync();

        return products.Select(MapToDTO);
    }


    // =====================================================
    // GET BY ID
    // PUBLIC
    // =====================================================

    public async Task<ProductResponseDTO?> GetByIdAsync(long id)
    {
        var product =
            await _unitOfWork.Products.GetByIdAsync(id);

        if (product == null)
        {
            return null;
        }

        return MapToDTO(product);
    }


    // =====================================================
    // GET BY CATEGORY
    // PUBLIC
    // =====================================================

    public async Task<IEnumerable<ProductResponseDTO>>
        GetByCategoryAsync(long categoryId)
    {
        // -------------------------------------------------
        // 1. CHECK CATEGORY
        // -------------------------------------------------

        var categoryExists =
            await _unitOfWork.Categories.AnyAsync(
                c => c.Id == categoryId);

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại.");
        }


        // -------------------------------------------------
        // 2. GET PRODUCTS
        // -------------------------------------------------

        var products =
            await _unitOfWork.Products.FindAsync(
                p => p.CategoryId == categoryId);

        return products.Select(MapToDTO);
    }


    // =====================================================
    // SEARCH
    // PUBLIC
    // =====================================================

    public async Task<IEnumerable<ProductResponseDTO>>
        SearchAsync(string keyword)
    {
        // -------------------------------------------------
        // 1. VALIDATE KEYWORD
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new BadRequestException(
                "Keyword không được để trống.");
        }

        keyword = keyword.Trim();


        // -------------------------------------------------
        // 2. SEARCH
        // -------------------------------------------------

        var products =
            await _unitOfWork.Products.FindAsync(
                p =>
                    p.Name.Contains(keyword) ||
                    p.Slug.Contains(keyword));

        return products.Select(MapToDTO);
    }


    // =====================================================
    // CREATE
    // SELLER
    // =====================================================

    public async Task<ProductResponseDTO>
        CreateAsync(
            long userId,
            CreateProductDTO request)
    {
        // -------------------------------------------------
        // 1. VALIDATE REQUEST
        // -------------------------------------------------

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }


        // -------------------------------------------------
        // 2. CHECK SHOP
        // -------------------------------------------------

        await GetApprovedOwnedShopAsync(
            userId,
            request.ShopId);


        // -------------------------------------------------
        // 3. VALIDATE NAME
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "Tên Product không được để trống.");
        }


        // -------------------------------------------------
        // 4. VALIDATE SLUG
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống.");
        }


        // -------------------------------------------------
        // 5. CHECK CATEGORY
        // -------------------------------------------------

        var categoryExists =
            await _unitOfWork.Categories.AnyAsync(
                c => c.Id == request.CategoryId);

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại.");
        }


        // -------------------------------------------------
        // 6. CHECK DUPLICATE SLUG
        // -------------------------------------------------

        var slug =
            request.Slug
                .Trim()
                .ToLowerInvariant();

        var slugExists =
            await _unitOfWork.Products.AnyAsync(
                p => p.Slug == slug);

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{slug}' đã tồn tại.");
        }


        // -------------------------------------------------
        // 7. CREATE PRODUCT
        // -------------------------------------------------

        var product = new ProductEntity
        {
            ShopId = request.ShopId,

            CategoryId = request.CategoryId,

            Name = request.Name.Trim(),

            Slug = slug,

            Description =
                request.Description?.Trim(),

            Status =
                string.IsNullOrWhiteSpace(request.Status)
                    ? "active"
                    : request.Status
                        .Trim()
                        .ToLowerInvariant(),

            CreatedAt = DateTime.UtcNow
        };


        // -------------------------------------------------
        // 8. ADD PRODUCT
        // -------------------------------------------------

        await _unitOfWork.Products.AddAsync(product);


        // -------------------------------------------------
        // 9. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 10. RETURN
        // -------------------------------------------------

        return MapToDTO(product);
    }


    // =====================================================
    // UPDATE
    // SELLER
    // =====================================================

    public async Task<ProductResponseDTO?>
        UpdateAsync(
            long userId,
            long id,
            UpdateProductDTO request)
    {
        // -------------------------------------------------
        // 1. VALIDATE REQUEST
        // -------------------------------------------------

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }


        // -------------------------------------------------
        // 2. FIND PRODUCT
        // -------------------------------------------------

        var product =
            await _unitOfWork.Products.GetByIdAsync(id);

        if (product == null)
        {
            return null;
        }


        // -------------------------------------------------
        // 3. CHECK SHOP
        // -------------------------------------------------
        //
        // Shop trong request phải:
        // - tồn tại
        // - thuộc Seller hiện tại
        // - đã được Admin approve
        //

        await GetApprovedOwnedShopAsync(
            userId,
            request.ShopId);


        // -------------------------------------------------
        // 4. VALIDATE NAME
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "Tên Product không được để trống.");
        }


        // -------------------------------------------------
        // 5. VALIDATE SLUG
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống.");
        }


        // -------------------------------------------------
        // 6. CHECK CATEGORY
        // -------------------------------------------------

        var categoryExists =
            await _unitOfWork.Categories.AnyAsync(
                c => c.Id == request.CategoryId);

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại.");
        }


        // -------------------------------------------------
        // 7. CHECK DUPLICATE SLUG
        // -------------------------------------------------

        var slug =
            request.Slug
                .Trim()
                .ToLowerInvariant();

        var slugExists =
            await _unitOfWork.Products.AnyAsync(
                p =>
                    p.Slug == slug &&
                    p.Id != id);

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{slug}' đã tồn tại.");
        }


        // -------------------------------------------------
        // 8. UPDATE PRODUCT
        // -------------------------------------------------

        product.ShopId =
            request.ShopId;

        product.CategoryId =
            request.CategoryId;

        product.Name =
            request.Name.Trim();

        product.Slug =
            slug;

        product.Description =
            request.Description?.Trim();

        product.Status =
            string.IsNullOrWhiteSpace(request.Status)
                ? "active"
                : request.Status
                    .Trim()
                    .ToLowerInvariant();

        product.UpdatedAt =
            DateTime.UtcNow;


        // -------------------------------------------------
        // 9. UPDATE REPOSITORY
        // -------------------------------------------------

        _unitOfWork.Products.Update(product);


        // -------------------------------------------------
        // 10. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 11. RETURN
        // -------------------------------------------------

        return MapToDTO(product);
    }


    // =====================================================
    // DELETE
    // SELLER
    // =====================================================

    public async Task<bool>
        DeleteAsync(
            long userId,
            long id)
    {
        // -------------------------------------------------
        // 1. FIND PRODUCT
        // -------------------------------------------------

        var product =
            await _unitOfWork.Products.GetByIdAsync(id);

        if (product == null)
        {
            return false;
        }


        // -------------------------------------------------
        // 2. CHECK SHOP
        // -------------------------------------------------
        //
        // Product đang thuộc Shop nào thì Shop đó phải:
        // - tồn tại
        // - thuộc Seller hiện tại
        // - approved
        //

        await GetApprovedOwnedShopAsync(
            userId,
            product.ShopId);


        // -------------------------------------------------
        // 3. CHECK PRODUCT VARIANT
        // -------------------------------------------------

        var hasVariants =
            await _unitOfWork.ProductVariants.AnyAsync(
                v => v.ProductId == id);

        if (hasVariants)
        {
            throw new BadRequestException(
                "Không thể xóa Product đang có ProductVariant.");
        }


        // -------------------------------------------------
        // 4. DELETE
        // -------------------------------------------------

        _unitOfWork.Products.Delete(product);


        // -------------------------------------------------
        // 5. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        return true;
    }


    // =====================================================
    // CHECK SHOP
    //
    // Shop phải:
    // 1. Tồn tại
    // 2. Thuộc Seller hiện tại
    // 3. Đã được Admin approve
    // =====================================================

    private async Task<MiniLogistics.DAL.Models.Shop>
        GetApprovedOwnedShopAsync(
            long userId,
            long shopId)
    {
        // -------------------------------------------------
        // 1. VALIDATE USER ID
        // -------------------------------------------------

        if (userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");
        }


        // -------------------------------------------------
        // 2. VALIDATE SHOP ID
        // -------------------------------------------------

        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }


        // -------------------------------------------------
        // 3. FIND SHOP
        // -------------------------------------------------

        var shop =
            await _unitOfWork.Shops.GetByIdAsync(
                shopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }


        // -------------------------------------------------
        // 4. CHECK OWNER
        // -------------------------------------------------

        if (shop.OwnerUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền thao tác với Shop này.");
        }


        // -------------------------------------------------
        // 5. CHECK APPROVAL
        // -------------------------------------------------

        if (!string.Equals(
                shop.Status?.Trim(),
                "approved",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                $"Shop chưa được Admin duyệt. " +
                $"Trạng thái hiện tại: '{shop.Status}'.");
        }


        // -------------------------------------------------
        // 6. RETURN SHOP
        // -------------------------------------------------

        return shop;
    }


    // =====================================================
    // MAP ENTITY -> DTO
    // =====================================================

    private static ProductResponseDTO
        MapToDTO(ProductEntity product)
    {
        return new ProductResponseDTO
        {
            Id = product.Id,

            ShopId = product.ShopId,

            CategoryId = product.CategoryId,

            Name = product.Name,

            Slug = product.Slug,

            Description = product.Description,

            Status = product.Status,

            CreatedAt = product.CreatedAt,

            UpdatedAt = product.UpdatedAt
        };
    }
}