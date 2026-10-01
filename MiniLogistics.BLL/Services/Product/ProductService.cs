using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

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
        if (request == null)
        {
            request = new ProductPaginationRequestDTO();
        }

        if (request.Page < 1)
            request.Page = 1;

        if (request.PageSize < 1)
            request.PageSize = 10;

        if (request.PageSize > 100)
            request.PageSize = 100;

        var search = request.Search?.Trim().ToLower();
        var status = request.Status?.Trim().ToLower();

        Expression<Func<ProductEntity, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            predicate = product =>
                product.Name.ToLower().Contains(search)
                || product.Slug.ToLower().Contains(search)
                || (product.Description != null
                    && product.Description.ToLower().Contains(search));
        }

        if (request.CategoryId.HasValue)
        {
            var categoryId = request.CategoryId.Value;
            Expression<Func<ProductEntity, bool>> categoryPredicate =
                product => product.CategoryId == categoryId;
            predicate = CombinePredicates(predicate, categoryPredicate);
        }

        if (request.ShopId.HasValue)
        {
            var shopId = request.ShopId.Value;
            Expression<Func<ProductEntity, bool>> shopPredicate =
                product => product.ShopId == shopId;
            predicate = CombinePredicates(predicate, shopPredicate);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            Expression<Func<ProductEntity, bool>> statusPredicate =
                product => product.Status.ToLower().Equals(status);
            predicate = CombinePredicates(predicate, statusPredicate);
        }

        var query = _unitOfWork.Products.Query()
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.ProductVariants)
            .AsSplitQuery()
            .AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalItems = await query.CountAsync();

        var entities = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var items = entities.Select(MapToDTO).ToList();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)request.PageSize);

        return new PagedResponseDTO<ProductResponseDTO>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
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
        var product = await _unitOfWork.Products.Query()
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == id);

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

        var products = await _unitOfWork.Products.Query()
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.ProductVariants)
            .Where(p => p.CategoryId == categoryId)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return products.Select(MapToDTO);
    }


    // =====================================================
    // SEARCH
    // PUBLIC
    // =====================================================

    public async Task<IEnumerable<ProductResponseDTO>>
        SearchAsync(
            string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new BadRequestException(
                "Keyword không được để trống.");
        }

        keyword = keyword.Trim();

        var products = await _unitOfWork.Products.Query()
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.ProductVariants)
            .Where(p =>
                p.Name.Contains(keyword)
                || p.Slug.Contains(keyword)
                || (p.Description != null && p.Description.Contains(keyword)))
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return products.Select(MapToDTO);
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
        var imageUrl = product.ProductImages?
            .OrderBy(i => i.SortOrder)
            .Select(i => i.Url)
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

        var minPrice = product.ProductVariants?
            .Where(v => v.IsActive)
            .Select(v => (decimal?)v.Price)
            .DefaultIfEmpty()
            .Min();

        if (minPrice is null && product.ProductVariants?.Count > 0)
        {
            minPrice = product.ProductVariants.Min(v => v.Price);
        }

        return new ProductResponseDTO
        {
            Id = product.Id,
            ShopId = product.ShopId,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Status = product.Status,
            ImageUrl = imageUrl,
            MinPrice = minPrice,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}