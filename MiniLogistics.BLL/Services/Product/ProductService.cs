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
    // =====================================================

    public async Task<IEnumerable<ProductResponseDTO>> GetAllAsync()
    {
        var products =
            await _unitOfWork.Products.GetAllAsync();

        return products.Select(MapToDTO);
    }


    // =====================================================
    // GET BY ID
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
    // =====================================================

    public async Task<IEnumerable<ProductResponseDTO>>
        GetByCategoryAsync(long categoryId)
    {
        // -------------------------------------------------
        // CHECK CATEGORY
        // -------------------------------------------------

        var categoryExists =
            await _unitOfWork.Categories.AnyAsync(
                c => c.Id == categoryId
            );

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại."
            );
        }


        // -------------------------------------------------
        // GET PRODUCTS
        // -------------------------------------------------

        var products =
            await _unitOfWork.Products.FindAsync(
                p => p.CategoryId == categoryId
            );

        return products.Select(MapToDTO);
    }


    // =====================================================
    // SEARCH
    // =====================================================

    public async Task<IEnumerable<ProductResponseDTO>>
        SearchAsync(string keyword)
    {
        // -------------------------------------------------
        // VALIDATE KEYWORD
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new BadRequestException(
                "Keyword không được để trống."
            );
        }

        keyword = keyword.Trim();


        // -------------------------------------------------
        // SEARCH
        // -------------------------------------------------

        var products =
            await _unitOfWork.Products.FindAsync(
                p =>
                    p.Name.Contains(keyword) ||
                    p.Slug.Contains(keyword)
            );

        return products.Select(MapToDTO);
    }


    // =====================================================
    // CREATE
    // =====================================================

    public async Task<ProductResponseDTO>
        CreateAsync(CreateProductDTO request)
    {
        // -------------------------------------------------
        // 1. VALIDATE NAME
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "Tên Product không được để trống."
            );
        }


        // -------------------------------------------------
        // 2. VALIDATE SLUG
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống."
            );
        }


        // -------------------------------------------------
        // 3. CHECK CATEGORY
        // -------------------------------------------------

        var categoryExists =
            await _unitOfWork.Categories.AnyAsync(
                c => c.Id == request.CategoryId
            );

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại."
            );
        }


        // -------------------------------------------------
        // 4. CHECK DUPLICATE SLUG
        // -------------------------------------------------

        var slug =
            request.Slug
                .Trim()
                .ToLowerInvariant();

        var slugExists =
            await _unitOfWork.Products.AnyAsync(
                p => p.Slug == slug
            );

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{slug}' đã tồn tại."
            );
        }


        // -------------------------------------------------
        // 5. CREATE PRODUCT
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
        // 6. ADD
        // -------------------------------------------------

        await _unitOfWork.Products.AddAsync(product);


        // -------------------------------------------------
        // 7. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 8. RETURN
        // -------------------------------------------------

        return MapToDTO(product);
    }


    // =====================================================
    // UPDATE
    // =====================================================

    public async Task<ProductResponseDTO?>
        UpdateAsync(
            long id,
            UpdateProductDTO request)
    {
        // -------------------------------------------------
        // 1. FIND PRODUCT
        // -------------------------------------------------

        var product =
            await _unitOfWork.Products.GetByIdAsync(id);

        if (product == null)
        {
            return null;
        }


        // -------------------------------------------------
        // 2. VALIDATE NAME
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "Tên Product không được để trống."
            );
        }


        // -------------------------------------------------
        // 3. VALIDATE SLUG
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống."
            );
        }


        // -------------------------------------------------
        // 4. CHECK CATEGORY
        // -------------------------------------------------

        var categoryExists =
            await _unitOfWork.Categories.AnyAsync(
                c => c.Id == request.CategoryId
            );

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại."
            );
        }


        // -------------------------------------------------
        // 5. CHECK DUPLICATE SLUG
        // -------------------------------------------------

        var slug =
            request.Slug
                .Trim()
                .ToLowerInvariant();

        var slugExists =
            await _unitOfWork.Products.AnyAsync(
                p =>
                    p.Slug == slug &&
                    p.Id != id
            );

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{slug}' đã tồn tại."
            );
        }


        // -------------------------------------------------
        // 6. UPDATE PRODUCT
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
        // 7. UPDATE
        // -------------------------------------------------

        _unitOfWork.Products.Update(product);


        // -------------------------------------------------
        // 8. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 9. RETURN
        // -------------------------------------------------

        return MapToDTO(product);
    }


    // =====================================================
    // DELETE
    // =====================================================

    public async Task<bool> DeleteAsync(long id)
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
        // 2. CHECK PRODUCT VARIANT
        // -------------------------------------------------

        var hasVariants =
            await _unitOfWork.ProductVariants.AnyAsync(
                v => v.ProductId == id
            );

        if (hasVariants)
        {
            throw new BadRequestException(
                "Không thể xóa Product đang có ProductVariant."
            );
        }


        // -------------------------------------------------
        // 3. DELETE
        // -------------------------------------------------

        _unitOfWork.Products.Delete(product);


        // -------------------------------------------------
        // 4. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        return true;
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