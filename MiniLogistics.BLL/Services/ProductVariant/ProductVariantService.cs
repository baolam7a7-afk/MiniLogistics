using System.Linq.Expressions;

using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.ProductVariant;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using ProductModel =
    MiniLogistics.DAL.Models.Product;

using ProductVariantModel =
    MiniLogistics.DAL.Models.ProductVariant;

using InventoryModel =
    MiniLogistics.DAL.Models.Inventory;

namespace MiniLogistics.BLL.Services;

public class ProductVariantService : IProductVariantService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductVariantService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // PUBLIC
    // PAGINATION
    // =====================================================

    public async Task<PagedResponseDTO<ProductVariantResponseDTO>>
        GetAllAsync(
            ProductVariantPaginationRequestDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được để trống.");
        }

        if (request.Page <= 0)
        {
            throw new BadRequestException(
                "Page phải lớn hơn 0.");
        }

        if (request.PageSize <= 0)
        {
            throw new BadRequestException(
                "PageSize phải lớn hơn 0.");
        }


        // -------------------------------------------------
        // Tạo điều kiện filter
        // -------------------------------------------------

        Expression<Func<ProductVariantModel, bool>>? predicate = null;


        // -------------------------------------------------
        // Search theo SKU hoặc VariantName
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword =
                request.Search.Trim().ToLower();

            predicate = x =>
                (x.Sku != null &&
                 x.Sku.ToLower().Contains(keyword))
                ||
                (x.VariantName != null &&
                 x.VariantName
                    .ToLower()
                    .Contains(keyword));
        }


        // -------------------------------------------------
        // Filter theo ProductId
        // -------------------------------------------------

        if (request.ProductId.HasValue)
        {
            var productId =
                request.ProductId.Value;

            Expression<Func<ProductVariantModel, bool>>
                productPredicate =
                    x => x.ProductId == productId;

            predicate =
                CombinePredicates(
                    predicate,
                    productPredicate);
        }


        // -------------------------------------------------
        // Filter theo IsActive
        // -------------------------------------------------

        if (request.IsActive.HasValue)
        {
            var isActive =
                request.IsActive.Value;

            Expression<Func<ProductVariantModel, bool>>
                activePredicate =
                    x => x.IsActive == isActive;

            predicate =
                CombinePredicates(
                    predicate,
                    activePredicate);
        }


        // -------------------------------------------------
        // Pagination
        // -------------------------------------------------

        var (variants, totalItems) =
            await _unitOfWork.ProductVariants.GetPagedAsync(
                request.Page,
                request.PageSize,
                predicate,
                query =>
                    query.OrderByDescending(
                        x => x.Id));


        // -------------------------------------------------
        // Map Response
        // -------------------------------------------------

        var result =
            new List<ProductVariantResponseDTO>();

        foreach (var variant in variants)
        {
            var inventories =
                await _unitOfWork.Inventories.FindAsync(
                    x =>
                        x.ProductVariantId ==
                        variant.Id);

            var inventory =
                inventories.FirstOrDefault();

            result.Add(
                MapToResponseDTO(
                    variant,
                    inventory));
        }


        // -------------------------------------------------
        // Tính TotalPages
        // -------------------------------------------------

        var totalPages =
            (int)Math.Ceiling(
                totalItems /
                (double)request.PageSize);


        // -------------------------------------------------
        // Response
        // -------------------------------------------------

        return new PagedResponseDTO<ProductVariantResponseDTO>
        {
            Items = result,

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


    // =====================================================
    // GET BY ID
    // PUBLIC
    // =====================================================

    public async Task<ProductVariantResponseDTO?>
        GetByIdAsync(long id)
    {
        if (id <= 0)
        {
            throw new BadRequestException(
                "ProductVariant ID không hợp lệ.");
        }

        var variant =
            await _unitOfWork.ProductVariants
                .GetByIdAsync(id);

        if (variant == null)
        {
            return null;
        }

        var inventories =
            await _unitOfWork.Inventories.FindAsync(
                x =>
                    x.ProductVariantId ==
                    variant.Id);

        var inventory =
            inventories.FirstOrDefault();

        return MapToResponseDTO(
            variant,
            inventory);
    }


    // =====================================================
    // GET BY PRODUCT ID
    // PUBLIC
    // =====================================================

    public async Task<IEnumerable<ProductVariantResponseDTO>>
        GetByProductIdAsync(
            long productId)
    {
        if (productId <= 0)
        {
            throw new BadRequestException(
                "ProductId không hợp lệ.");
        }

        var variants =
            await _unitOfWork.ProductVariants
                .FindAsync(
                    variant =>
                        variant.ProductId ==
                        productId);

        var result =
            new List<ProductVariantResponseDTO>();

        foreach (var variant in variants)
        {
            var inventories =
                await _unitOfWork.Inventories.FindAsync(
                    x =>
                        x.ProductVariantId ==
                        variant.Id);

            var inventory =
                inventories.FirstOrDefault();

            result.Add(
                MapToResponseDTO(
                    variant,
                    inventory));
        }

        return result;
    }


    // =====================================================
    // CREATE
    // SELLER
    // =====================================================

    public async Task<ProductVariantResponseDTO>
        CreateAsync(
            long userId,
            CreateProductVariantDTO request)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");
        }

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được để trống.");
        }


        // -------------------------------------------------
        // 1. Validate ProductId
        // -------------------------------------------------

        if (request.ProductId <= 0)
        {
            throw new BadRequestException(
                "ProductId không hợp lệ.");
        }


        // -------------------------------------------------
        // 2. Tìm Product
        // -------------------------------------------------

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(
                    request.ProductId);

        if (product == null)
        {
            throw new NotFoundException(
                $"Product với ID {request.ProductId} không tồn tại.");
        }


        // -------------------------------------------------
        // 3. Kiểm tra quyền Seller với Shop
        // -------------------------------------------------

        await CheckProductShopAccessAsync(
            product,
            userId);


        // -------------------------------------------------
        // 4. Validate SKU
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                request.Sku))
        {
            throw new BadRequestException(
                "SKU không được để trống.");
        }

        var sku =
            request.Sku.Trim();

        var existingVariants =
            await _unitOfWork.ProductVariants
                .FindAsync(
                    x =>
                        x.Sku != null &&
                        x.Sku.ToLower()
                            == sku.ToLower());

        if (existingVariants.Any())
        {
            throw new BadRequestException(
                $"SKU '{sku}' đã tồn tại.");
        }


        // -------------------------------------------------
        // 5. Validate Price
        // -------------------------------------------------

        if (request.Price < 0)
        {
            throw new BadRequestException(
                "Price không được nhỏ hơn 0.");
        }


        // -------------------------------------------------
        // 6. Tạo ProductVariant
        // -------------------------------------------------

        var variant =
            new ProductVariantModel
            {
                ProductId =
                    request.ProductId,

                Sku =
                    sku,

                VariantName =
                    request.VariantName,

                AttributesJson =
                    request.AttributesJson,

                Price =
                    request.Price,

                IsActive =
                    request.IsActive,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    null
            };


        // -------------------------------------------------
        // 7. Tạo Inventory
        // -------------------------------------------------

        var inventory =
            new InventoryModel
            {
                ProductVariant =
                    variant,

                Quantity =
                    0,

                ReservedQuantity =
                    0,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    null
            };


        // -------------------------------------------------
        // 8. Add ProductVariant
        // -------------------------------------------------

        await _unitOfWork.ProductVariants
            .AddAsync(variant);


        // -------------------------------------------------
        // 9. Add Inventory
        // -------------------------------------------------

        await _unitOfWork.Inventories
            .AddAsync(inventory);


        // -------------------------------------------------
        // 10. Save
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 11. Response
        // -------------------------------------------------

        return MapToResponseDTO(
            variant,
            inventory);
    }


    // =====================================================
    // UPDATE
    // SELLER
    // =====================================================

    public async Task<ProductVariantResponseDTO?>
        UpdateAsync(
            long userId,
            long id,
            UpdateProductVariantDTO request)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");
        }

        if (id <= 0)
        {
            throw new BadRequestException(
                "ProductVariant ID không hợp lệ.");
        }

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được để trống.");
        }


        // -------------------------------------------------
        // 1. Tìm ProductVariant
        // -------------------------------------------------

        var variant =
            await _unitOfWork.ProductVariants
                .GetByIdAsync(id);

        if (variant == null)
        {
            return null;
        }


        // -------------------------------------------------
        // 2. Tìm Product
        // -------------------------------------------------

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(
                    variant.ProductId);

        if (product == null)
        {
            throw new NotFoundException(
                $"Product với ID {variant.ProductId} không tồn tại.");
        }


        // -------------------------------------------------
        // 3. Kiểm tra quyền Seller
        // -------------------------------------------------

        await CheckProductShopAccessAsync(
            product,
            userId);


        // -------------------------------------------------
        // 4. Validate SKU
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                request.Sku))
        {
            throw new BadRequestException(
                "SKU không được để trống.");
        }

        var sku =
            request.Sku.Trim();

        var duplicateVariants =
            await _unitOfWork.ProductVariants
                .FindAsync(
                    x =>
                        x.Id != id &&
                        x.Sku != null &&
                        x.Sku.ToLower()
                            == sku.ToLower());

        if (duplicateVariants.Any())
        {
            throw new BadRequestException(
                $"SKU '{sku}' đã tồn tại.");
        }


        // -------------------------------------------------
        // 5. Validate Price
        // -------------------------------------------------

        if (request.Price < 0)
        {
            throw new BadRequestException(
                "Price không được nhỏ hơn 0.");
        }


        // -------------------------------------------------
        // 6. Update
        // -------------------------------------------------

        variant.Sku =
            sku;

        variant.VariantName =
            request.VariantName;

        variant.AttributesJson =
            request.AttributesJson;

        variant.Price =
            request.Price;

        variant.IsActive =
            request.IsActive;

        variant.UpdatedAt =
            DateTime.UtcNow;


        // -------------------------------------------------
        // 7. Update Repository
        // -------------------------------------------------

        _unitOfWork.ProductVariants
            .Update(variant);


        // -------------------------------------------------
        // 8. Save
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 9. Get Inventory
        // -------------------------------------------------

        var inventories =
            await _unitOfWork.Inventories.FindAsync(
                x =>
                    x.ProductVariantId ==
                    variant.Id);

        var inventory =
            inventories.FirstOrDefault();


        // -------------------------------------------------
        // 10. Response
        // -------------------------------------------------

        return MapToResponseDTO(
            variant,
            inventory);
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
        if (userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");
        }

        if (id <= 0)
        {
            throw new BadRequestException(
                "ProductVariant ID không hợp lệ.");
        }


        // -------------------------------------------------
        // 1. Tìm ProductVariant
        // -------------------------------------------------

        var variant =
            await _unitOfWork.ProductVariants
                .GetByIdAsync(id);

        if (variant == null)
        {
            return false;
        }


        // -------------------------------------------------
        // 2. Tìm Product
        // -------------------------------------------------

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(
                    variant.ProductId);

        if (product == null)
        {
            throw new NotFoundException(
                $"Product với ID {variant.ProductId} không tồn tại.");
        }


        // -------------------------------------------------
        // 3. Kiểm tra quyền Seller
        // -------------------------------------------------

        await CheckProductShopAccessAsync(
            product,
            userId);


        // -------------------------------------------------
        // 4. Tìm Inventory
        // -------------------------------------------------

        var inventories =
            await _unitOfWork.Inventories
                .FindAsync(
                    x =>
                        x.ProductVariantId ==
                        id);

        var inventory =
            inventories.FirstOrDefault();


        // -------------------------------------------------
        // 5. Xóa Inventory trước
        // -------------------------------------------------

        if (inventory != null)
        {
            _unitOfWork.Inventories
                .Delete(inventory);
        }


        // -------------------------------------------------
        // 6. Xóa ProductVariant
        // -------------------------------------------------

        _unitOfWork.ProductVariants
            .Delete(variant);


        // -------------------------------------------------
        // 7. Save
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();

        return true;
    }


    // =====================================================
    // CHECK PRODUCT SHOP ACCESS
    // =====================================================

    private async Task CheckProductShopAccessAsync(
        ProductModel product,
        long userId)
    {
        if (product.ShopId <= 0)
        {
            throw new BadRequestException(
                "Product chưa được gắn với Shop hợp lệ.");
        }


        // -------------------------------------------------
        // Tìm Shop
        // -------------------------------------------------

        var shop =
            await _unitOfWork.Shops
                .GetByIdAsync(
                    product.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {product.ShopId} không tồn tại.");
        }


        // -------------------------------------------------
        // Kiểm tra Owner
        // -------------------------------------------------

        if (shop.OwnerUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền quản lý ProductVariant của Shop này.");
        }


        // -------------------------------------------------
        // Kiểm tra Shop đã được Admin duyệt
        // -------------------------------------------------

        if (!string.Equals(
                shop.Status?.Trim(),
                "approved",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                $"Shop chưa được Admin duyệt. Trạng thái hiện tại: '{shop.Status}'.");
        }
    }


    // =====================================================
    // MAP DTO
    // =====================================================

    private ProductVariantResponseDTO
        MapToResponseDTO(
            ProductVariantModel variant,
            InventoryModel? inventory)
    {
        int quantity =
            inventory?.Quantity ?? 0;

        int reservedQuantity =
            inventory?.ReservedQuantity ?? 0;

        int availableQuantity =
            quantity -
            reservedQuantity;


        return new ProductVariantResponseDTO
        {
            Id =
                variant.Id,

            ProductId =
                variant.ProductId,

            Sku =
                variant.Sku,

            VariantName =
                variant.VariantName,

            AttributesJson =
                variant.AttributesJson,

            Price =
                variant.Price,

            Stock =
                availableQuantity,

            IsActive =
                variant.IsActive,

            CreatedAt =
                variant.CreatedAt,

            UpdatedAt =
                variant.UpdatedAt
        };
    }


    // =====================================================
    // COMBINE PREDICATES
    // =====================================================

    private static Expression<Func<ProductVariantModel, bool>>
        CombinePredicates(
            Expression<Func<ProductVariantModel, bool>>? first,
            Expression<Func<ProductVariantModel, bool>> second)
    {
        if (first == null)
        {
            return second;
        }

        var parameter =
            Expression.Parameter(
                typeof(ProductVariantModel),
                "x");

        var firstBody =
            new ParameterReplacer(
                first.Parameters[0],
                parameter)
            .Visit(first.Body);

        var secondBody =
            new ParameterReplacer(
                second.Parameters[0],
                parameter)
            .Visit(second.Body);

        var body =
            Expression.AndAlso(
                firstBody!,
                secondBody!);

        return Expression.Lambda<
            Func<ProductVariantModel, bool>>(
                body,
                parameter);
    }


    // =====================================================
    // PARAMETER REPLACER
    // =====================================================

    private sealed class ParameterReplacer
        : ExpressionVisitor
    {
        private readonly ParameterExpression
            _oldParameter;

        private readonly ParameterExpression
            _newParameter;

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
}