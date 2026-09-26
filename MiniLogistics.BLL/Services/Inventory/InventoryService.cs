using System.Linq.Expressions;

using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Inventory;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

using InventoryModel =
    MiniLogistics.DAL.Models.Inventory;

namespace MiniLogistics.BLL.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // PAGINATION
    // =====================================================

    public async Task<PagedResponseDTO<InventoryResponseDTO>>
        GetAllAsync(
            InventoryPaginationRequestDTO request)
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


        Expression<Func<InventoryModel, bool>>?
            predicate = null;


        // -------------------------------------------------
        // Filter ProductVariantId
        // -------------------------------------------------

        if (request.ProductVariantId.HasValue)
        {
            var productVariantId =
                request.ProductVariantId.Value;

            predicate =
                x =>
                    x.ProductVariantId ==
                    productVariantId;
        }


        // -------------------------------------------------
        // Filter IsActive
        //
        // Inventory không có IsActive.
        // IsActive thuộc ProductVariant.
        // Vì vậy filter này sẽ xử lý sau khi lấy
        // inventory theo ProductVariant.
        // -------------------------------------------------


        // -------------------------------------------------
        // Pagination
        // -------------------------------------------------

        var (inventories, totalItems) =
            await _unitOfWork.Inventories.GetPagedAsync(
                request.Page,
                request.PageSize,
                predicate,
                query =>
                    query.OrderByDescending(
                        x => x.Id));


        var result =
            new List<InventoryResponseDTO>();


        // -------------------------------------------------
        // Map Inventory + Variant
        // -------------------------------------------------

        foreach (var inventory in inventories)
        {
            var variant =
                await _unitOfWork.ProductVariants
                    .GetByIdAsync(
                        inventory.ProductVariantId);

            if (variant == null)
            {
                continue;
            }


            // -------------------------------------------------
            // Filter IsActive
            // -------------------------------------------------

            if (request.IsActive.HasValue &&
                variant.IsActive !=
                request.IsActive.Value)
            {
                continue;
            }


            // -------------------------------------------------
            // Search
            // -------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    request.Search))
            {
                var keyword =
                    request.Search
                        .Trim()
                        .ToLower();

                var sku =
                    variant.Sku?
                        .ToLower() ?? "";

                var variantName =
                    variant.VariantName?
                        .ToLower() ?? "";

                if (!sku.Contains(keyword) &&
                    !variantName.Contains(keyword))
                {
                    continue;
                }
            }


            result.Add(
                MapToDTO(
                    inventory,
                    variant));
        }


        // -------------------------------------------------
        // TotalPages
        // -------------------------------------------------

        var totalPages =
            (int)Math.Ceiling(
                totalItems /
                (double)request.PageSize);


        return new PagedResponseDTO<InventoryResponseDTO>
        {
            Items =
                result,

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
    // GET BY VARIANT
    // =====================================================

    public async Task<InventoryResponseDTO>
        GetByVariantIdAsync(
            long productVariantId)
    {
        var variant =
            await _unitOfWork.ProductVariants
                .GetByIdAsync(
                    productVariantId);

        if (variant == null)
        {
            throw new NotFoundException(
                "ProductVariant không tồn tại.");
        }

        var inventory =
            await GetInventoryEntity(
                productVariantId);

        return MapToDTO(
            inventory,
            variant);
    }


    // =====================================================
    // INCREASE
    // =====================================================

    public async Task<InventoryResponseDTO>
        IncreaseAsync(
            long productVariantId,
            IncreaseInventoryDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được để trống.");
        }

        var variant =
            await GetVariant(
                productVariantId);

        var inventory =
            await GetInventoryEntity(
                productVariantId);


        // -------------------------------------------------
        // Validate quantity
        // -------------------------------------------------

        if (request.Quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng nhập kho phải lớn hơn 0.");
        }


        // -------------------------------------------------
        // Increase quantity
        // -------------------------------------------------

        inventory.Quantity +=
            request.Quantity;

        inventory.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Inventories
            .Update(inventory);

        await _unitOfWork
            .SaveChangesAsync();


        return MapToDTO(
            inventory,
            variant);
    }


    // =====================================================
    // DECREASE
    // =====================================================

    public async Task<InventoryResponseDTO>
        DecreaseAsync(
            long productVariantId,
            DecreaseInventoryDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được để trống.");
        }

        var variant =
            await GetVariant(
                productVariantId);

        var inventory =
            await GetInventoryEntity(
                productVariantId);


        // -------------------------------------------------
        // Validate quantity
        // -------------------------------------------------

        if (request.Quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng giảm kho phải lớn hơn 0.");
        }


        // -------------------------------------------------
        // Available quantity
        // -------------------------------------------------

        int available =
            inventory.Quantity -
            inventory.ReservedQuantity;


        // -------------------------------------------------
        // Không được giảm quá Available
        // -------------------------------------------------

        if (request.Quantity > available)
        {
            throw new BadRequestException(
                "Số lượng tồn kho khả dụng không đủ.");
        }


        // -------------------------------------------------
        // Decrease
        // -------------------------------------------------

        inventory.Quantity -=
            request.Quantity;

        inventory.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Inventories
            .Update(inventory);

        await _unitOfWork
            .SaveChangesAsync();


        return MapToDTO(
            inventory,
            variant);
    }


    // =====================================================
    // ADJUST
    // =====================================================

    public async Task<InventoryResponseDTO>
        AdjustAsync(
            long productVariantId,
            AdjustInventoryDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được để trống.");
        }

        var variant =
            await GetVariant(
                productVariantId);

        var inventory =
            await GetInventoryEntity(
                productVariantId);


        // -------------------------------------------------
        // Không được nhỏ hơn ReservedQuantity
        // -------------------------------------------------

        if (request.Quantity <
            inventory.ReservedQuantity)
        {
            throw new BadRequestException(
                "Quantity không được nhỏ hơn ReservedQuantity.");
        }


        // -------------------------------------------------
        // Adjust
        // -------------------------------------------------

        inventory.Quantity =
            request.Quantity;

        inventory.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Inventories
            .Update(inventory);

        await _unitOfWork
            .SaveChangesAsync();


        return MapToDTO(
            inventory,
            variant);
    }


    // =====================================================
    // RESERVE
    // =====================================================

    public async Task ReserveAsync(
        long productVariantId,
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng Reserve phải lớn hơn 0.");
        }


        var inventory =
            await GetInventoryEntity(
                productVariantId);


        // -------------------------------------------------
        // Available
        // -------------------------------------------------

        int available =
            inventory.Quantity -
            inventory.ReservedQuantity;


        // -------------------------------------------------
        // Không đủ hàng
        // -------------------------------------------------

        if (quantity > available)
        {
            throw new BadRequestException(
                "Không đủ hàng để Reserve.");
        }


        // -------------------------------------------------
        // Reserve
        // -------------------------------------------------

        inventory.ReservedQuantity +=
            quantity;

        inventory.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Inventories
            .Update(inventory);

        await _unitOfWork
            .SaveChangesAsync();
    }


    // =====================================================
    // RELEASE
    // =====================================================

    public async Task ReleaseAsync(
        long productVariantId,
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng Release phải lớn hơn 0.");
        }


        var inventory =
            await GetInventoryEntity(
                productVariantId);


        // -------------------------------------------------
        // Validate ReservedQuantity
        // -------------------------------------------------

        if (quantity >
            inventory.ReservedQuantity)
        {
            throw new BadRequestException(
                "Số lượng Release vượt quá ReservedQuantity.");
        }


        // -------------------------------------------------
        // Release
        // -------------------------------------------------

        inventory.ReservedQuantity -=
            quantity;

        inventory.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Inventories
            .Update(inventory);

        await _unitOfWork
            .SaveChangesAsync();
    }


    // =====================================================
    // DEDUCT
    // =====================================================

    public async Task DeductAsync(
        long productVariantId,
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng Deduct phải lớn hơn 0.");
        }


        var inventory =
            await GetInventoryEntity(
                productVariantId);


        // -------------------------------------------------
        // Validate ReservedQuantity
        // -------------------------------------------------

        if (quantity >
            inventory.ReservedQuantity)
        {
            throw new BadRequestException(
                "Số lượng Deduct vượt quá ReservedQuantity.");
        }


        // -------------------------------------------------
        // Validate Quantity
        // -------------------------------------------------

        if (quantity >
            inventory.Quantity)
        {
            throw new BadRequestException(
                "Quantity không đủ.");
        }


        // -------------------------------------------------
        // Deduct
        // -------------------------------------------------

        inventory.Quantity -=
            quantity;

        inventory.ReservedQuantity -=
            quantity;

        inventory.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Inventories
            .Update(inventory);

        await _unitOfWork
            .SaveChangesAsync();
    }


    // =====================================================
    // GET VARIANT
    // =====================================================

    private async Task<ProductVariant>
        GetVariant(
            long productVariantId)
    {
        var variant =
            await _unitOfWork.ProductVariants
                .GetByIdAsync(
                    productVariantId);

        if (variant == null)
        {
            throw new NotFoundException(
                "ProductVariant không tồn tại.");
        }

        return variant;
    }


    // =====================================================
    // GET INVENTORY
    // =====================================================

    private async Task<InventoryModel>
        GetInventoryEntity(
            long productVariantId)
    {
        var inventories =
            await _unitOfWork.Inventories
                .FindAsync(
                    x =>
                        x.ProductVariantId ==
                        productVariantId);

        var inventory =
            inventories.FirstOrDefault();

        if (inventory == null)
        {
            throw new NotFoundException(
                "Inventory không tồn tại.");
        }

        return inventory;
    }


    // =====================================================
    // MAPPING
    // =====================================================

    private InventoryResponseDTO
        MapToDTO(
            InventoryModel inventory,
            ProductVariant variant)
    {
        return new InventoryResponseDTO
        {
            Id =
                inventory.Id,

            ProductVariantId =
                inventory.ProductVariantId,

            Sku =
                variant.Sku,

            VariantName =
                variant.VariantName,

            Price =
                variant.Price,

            Quantity =
                inventory.Quantity,

            ReservedQuantity =
                inventory.ReservedQuantity,

            AvailableQuantity =
                inventory.Quantity -
                inventory.ReservedQuantity,

            IsActive =
                variant.IsActive,

            CreatedAt =
                inventory.CreatedAt,

            UpdatedAt =
                inventory.UpdatedAt
        };
    }
}