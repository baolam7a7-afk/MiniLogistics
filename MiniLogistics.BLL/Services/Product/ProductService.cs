using System.Linq.Expressions;

using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Product;
using MiniLogistics.BLL.Exceptions;

using MiniLogistics.DAL.UnitOfWork;

using ProductEntity = MiniLogistics.DAL.Models.Product;

namespace MiniLogistics.BLL.Services.Product;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // PUBLIC
    // PAGINATION + SEARCH + FILTER
    // =====================================================

    public async Task<PagedResponseDTO<ProductResponseDTO>>
        GetAllAsync(
            ProductPaginationRequestDTO request)
    {
        // =================================================
        // VALIDATE REQUEST
        // =================================================

        if (request == null)
        {
            request =
                new ProductPaginationRequestDTO();
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


        // =================================================
        // PREPARE FILTER
        // =================================================

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

        Expression<Func<ProductEntity, bool>>? predicate =
            null;


        // -------------------------------------------------
        // SEARCH
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(search))
        {
            predicate =
                product =>
                    product.Name
                        .ToLower()
                        .Contains(search)
                    ||
                    product.Slug
                        .ToLower()
                        .Contains(search);
        }


        // -------------------------------------------------
        // CATEGORY
        // -------------------------------------------------

        if (request.CategoryId.HasValue)
        {
            var categoryId =
                request.CategoryId.Value;

            Expression<Func<ProductEntity, bool>>
                categoryPredicate =
                    product =>
                        product.CategoryId == categoryId;

            predicate =
                CombinePredicates(
                    predicate,
                    categoryPredicate);
        }


        // -------------------------------------------------
        // SHOP
        // -------------------------------------------------

        if (request.ShopId.HasValue)
        {
            var shopId =
                request.ShopId.Value;

            Expression<Func<ProductEntity, bool>>
                shopPredicate =
                    product =>
                        product.ShopId == shopId;

            predicate =
                CombinePredicates(
                    predicate,
                    shopPredicate);
        }


        // -------------------------------------------------
        // STATUS
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(status))
        {
            Expression<Func<ProductEntity, bool>>
                statusPredicate =
                    product =>
                        product.Status
                            .ToLower()
                            .Equals(status);

            predicate =
                CombinePredicates(
                    predicate,
                    statusPredicate);
        }


        // =================================================
        // QUERY DATABASE
        // =================================================

        var result =
            await _unitOfWork.Products
                .GetPagedAsync(
                    request.Page,
                    request.PageSize,
                    predicate,
                    query =>
                        query.OrderByDescending(
                            x => x.Id));


        // =================================================
        // MAP
        // =================================================

        var items =
            result.Items
                .Select(MapToDTO)
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

        return new PagedResponseDTO<ProductResponseDTO>
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
    // GET BY ID
    // PUBLIC
    // =====================================================

    public async Task<ProductResponseDTO?>
        GetByIdAsync(
            long id)
    {
        var product =
            await _unitOfWork.Products
                .GetByIdAsync(id);

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
        GetByCategoryAsync(
            long categoryId)
    {
        // =================================================
        // CHECK CATEGORY
        // =================================================

        var categoryExists =
            await _unitOfWork.Categories
                .AnyAsync(
                    c => c.Id == categoryId);

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại.");
        }


        // =================================================
        // GET PRODUCTS
        // =================================================

        var products =
            await _unitOfWork.Products
                .FindAsync(
                    p =>
                        p.CategoryId == categoryId);


        return products
            .Select(MapToDTO);
    }


    // =====================================================
    // SEARCH
    // PUBLIC
    // =====================================================

    public async Task<IEnumerable<ProductResponseDTO>>
        SearchAsync(
            string keyword)
    {
        // =================================================
        // VALIDATE KEYWORD
        // =================================================

        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new BadRequestException(
                "Keyword không được để trống.");
        }

        keyword =
            keyword.Trim();


        // =================================================
        // SEARCH
        // =================================================

        var products =
            await _unitOfWork.Products
                .FindAsync(
                    p =>
                        p.Name.Contains(keyword)
                        ||
                        p.Slug.Contains(keyword));


        return products
            .Select(MapToDTO);
    }


    // =====================================================
    // CREATE
    // SELLER ONLY
    // =====================================================

    public async Task<ProductResponseDTO>
        CreateAsync(
            long userId,
            CreateProductDTO request)
    {
        // =================================================
        // VALIDATE REQUEST
        // =================================================

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }


        // =================================================
        // CHECK SHOP
        // =================================================

        await GetApprovedOwnedShopAsync(
            userId,
            request.ShopId);


        // =================================================
        // VALIDATE NAME
        // =================================================

        if (string.IsNullOrWhiteSpace(
                request.Name))
        {
            throw new BadRequestException(
                "Tên Product không được để trống.");
        }


        // =================================================
        // VALIDATE SLUG
        // =================================================

        if (string.IsNullOrWhiteSpace(
                request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống.");
        }


        // =================================================
        // CHECK CATEGORY
        // =================================================

        var categoryExists =
            await _unitOfWork.Categories
                .AnyAsync(
                    c =>
                        c.Id ==
                        request.CategoryId);

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại.");
        }


        // =================================================
        // CHECK DUPLICATE SLUG
        // =================================================

        var slug =
            request.Slug
                .Trim()
                .ToLowerInvariant();

        var slugExists =
            await _unitOfWork.Products
                .AnyAsync(
                    p =>
                        p.Slug == slug);

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{slug}' đã tồn tại.");
        }


        // =================================================
        // CREATE PRODUCT
        // =================================================

        var product =
            new ProductEntity
            {
                ShopId =
                    request.ShopId,

                CategoryId =
                    request.CategoryId,

                Name =
                    request.Name.Trim(),

                Slug =
                    slug,

                Description =
                    request.Description?.Trim(),

                Status =
                    string.IsNullOrWhiteSpace(
                        request.Status)
                        ? "active"
                        : request.Status
                            .Trim()
                            .ToLowerInvariant(),

                CreatedAt =
                    DateTime.UtcNow
            };


        // =================================================
        // ADD
        // =================================================

        await _unitOfWork.Products
            .AddAsync(product);


        // =================================================
        // SAVE
        // =================================================

        await _unitOfWork
            .SaveChangesAsync();


        // =================================================
        // RETURN
        // =================================================

        return MapToDTO(product);
    }


    // =====================================================
    // UPDATE
    // SELLER ONLY
    // =====================================================

    public async Task<ProductResponseDTO?>
        UpdateAsync(
            long userId,
            long id,
            UpdateProductDTO request)
    {
        // =================================================
        // VALIDATE REQUEST
        // =================================================

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }


        // =================================================
        // FIND PRODUCT
        // =================================================

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(id);

        if (product == null)
        {
            return null;
        }


        // =================================================
        // CHECK SHOP
        // =================================================

        await GetApprovedOwnedShopAsync(
            userId,
            request.ShopId);


        // =================================================
        // VALIDATE NAME
        // =================================================

        if (string.IsNullOrWhiteSpace(
                request.Name))
        {
            throw new BadRequestException(
                "Tên Product không được để trống.");
        }


        // =================================================
        // VALIDATE SLUG
        // =================================================

        if (string.IsNullOrWhiteSpace(
                request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống.");
        }


        // =================================================
        // CHECK CATEGORY
        // =================================================

        var categoryExists =
            await _unitOfWork.Categories
                .AnyAsync(
                    c =>
                        c.Id ==
                        request.CategoryId);

        if (!categoryExists)
        {
            throw new NotFoundException(
                "Category không tồn tại.");
        }


        // =================================================
        // CHECK DUPLICATE SLUG
        // =================================================

        var slug =
            request.Slug
                .Trim()
                .ToLowerInvariant();

        var slugExists =
            await _unitOfWork.Products
                .AnyAsync(
                    p =>
                        p.Slug == slug &&
                        p.Id != id);

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{slug}' đã tồn tại.");
        }


        // =================================================
        // UPDATE PRODUCT
        // =================================================

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
            string.IsNullOrWhiteSpace(
                request.Status)
                ? "active"
                : request.Status
                    .Trim()
                    .ToLowerInvariant();

        product.UpdatedAt =
            DateTime.UtcNow;


        // =================================================
        // UPDATE REPOSITORY
        // =================================================

        _unitOfWork.Products
            .Update(product);


        // =================================================
        // SAVE
        // =================================================

        await _unitOfWork
            .SaveChangesAsync();


        // =================================================
        // RETURN
        // =================================================

        return MapToDTO(product);
    }


    // =====================================================
    // DELETE
    // SELLER ONLY
    // =====================================================

    public async Task<bool>
        DeleteAsync(
            long userId,
            long id)
    {
        // =================================================
        // FIND PRODUCT
        // =================================================

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(id);

        if (product == null)
        {
            return false;
        }


        // =================================================
        // CHECK SHOP
        // =================================================

        await GetApprovedOwnedShopAsync(
            userId,
            product.ShopId);


        // =================================================
        // CHECK PRODUCT VARIANT
        // =================================================

        var hasVariants =
            await _unitOfWork.ProductVariants
                .AnyAsync(
                    v =>
                        v.ProductId == id);

        if (hasVariants)
        {
            throw new BadRequestException(
                "Không thể xóa Product đang có ProductVariant.");
        }


        // =================================================
        // DELETE
        // =================================================

        _unitOfWork.Products
            .Delete(product);


        // =================================================
        // SAVE
        // =================================================

        await _unitOfWork
            .SaveChangesAsync();


        return true;
    }


    // =====================================================
    // CHECK SHOP
    // =====================================================
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
        // =================================================
        // VALIDATE USER ID
        // =================================================

        if (userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");
        }


        // =================================================
        // VALIDATE SHOP ID
        // =================================================

        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }


        // =================================================
        // FIND SHOP
        // =================================================

        var shop =
            await _unitOfWork.Shops
                .GetByIdAsync(shopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }


        // =================================================
        // CHECK OWNER
        // =================================================

        if (shop.OwnerUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền thao tác với Shop này.");
        }


        // =================================================
        // CHECK APPROVAL
        // =================================================

        if (!string.Equals(
                shop.Status?.Trim(),
                "approved",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                $"Shop chưa được Admin duyệt. " +
                $"Trạng thái hiện tại: '{shop.Status}'.");
        }


        // =================================================
        // RETURN SHOP
        // =================================================

        return shop;
    }


    // =====================================================
    // COMBINE EXPRESSIONS
    // =====================================================

    private static Expression<Func<T, bool>>
        CombinePredicates<T>(
            Expression<Func<T, bool>>? first,
            Expression<Func<T, bool>> second)
    {
        if (first == null)
        {
            return second;
        }

        var parameter =
            Expression.Parameter(
                typeof(T),
                "x");

        var firstBody =
            ReplaceParameter(
                first.Body,
                first.Parameters[0],
                parameter);

        var secondBody =
            ReplaceParameter(
                second.Body,
                second.Parameters[0],
                parameter);

        var body =
            Expression.AndAlso(
                firstBody,
                secondBody);

        return Expression.Lambda<Func<T, bool>>(
            body,
            parameter);
    }


    private static Expression
        ReplaceParameter(
            Expression expression,
            ParameterExpression oldParameter,
            ParameterExpression newParameter)
    {
        return new ParameterReplacer(
            oldParameter,
            newParameter)
            .Visit(expression)!;
    }


    private sealed class ParameterReplacer
        : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(
            ParameterExpression oldParameter,
            ParameterExpression newParameter)
        {
            _oldParameter =
                oldParameter;

            _newParameter =
                newParameter;
        }

        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return node == _oldParameter
                ? _newParameter
                : base.VisitParameter(node);
        }
    }


    // =====================================================
    // MAP ENTITY -> DTO
    // =====================================================

    private static ProductResponseDTO
        MapToDTO(
            ProductEntity product)
    {
        return new ProductResponseDTO
        {
            Id =
                product.Id,

            ShopId =
                product.ShopId,

            CategoryId =
                product.CategoryId,

            Name =
                product.Name,

            Slug =
                product.Slug,

            Description =
                product.Description,

            Status =
                product.Status,

            CreatedAt =
                product.CreatedAt,

            UpdatedAt =
                product.UpdatedAt
        };
    }
}