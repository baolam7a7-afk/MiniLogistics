using MiniLogistics.BLL.DTOs.Inventory;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;
using InventoryModel = MiniLogistics.DAL.Models.Inventory;

namespace MiniLogistics.BLL.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // =====================================================

    public async Task<IEnumerable<InventoryResponseDTO>>
        GetAllAsync()
    {
        var inventories =
            await _unitOfWork.Inventories
                .GetAllAsync();

        var result =
            new List<InventoryResponseDTO>();

        foreach (var inventory in inventories)
        {
            var variant =
                await _unitOfWork.ProductVariants
                    .GetByIdAsync(
                        inventory.ProductVariantId
                    );

            if (variant == null)
            {
                continue;
            }

            result.Add(
                MapToDTO(
                    inventory,
                    variant
                )
            );
        }

        return result;
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
                    productVariantId
                );

        if (variant == null)
        {
            throw new NotFoundException(
                "ProductVariant không tồn tại."
            );
        }

        var inventory =
            await GetInventoryEntity(
                productVariantId
            );

        return MapToDTO(
            inventory,
            variant
        );
    }


    // =====================================================
    // INCREASE
    // =====================================================

    public async Task<InventoryResponseDTO>
        IncreaseAsync(
            long productVariantId,
            IncreaseInventoryDTO request)
    {
        var variant =
            await GetVariant(
                productVariantId
            );

        var inventory =
            await GetInventoryEntity(
                productVariantId
            );


        // -------------------------------------------------
        // Validate quantity
        // -------------------------------------------------

        if (request.Quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng nhập kho phải lớn hơn 0."
            );
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

        await _unitOfWork.SaveChangesAsync();


        return MapToDTO(
            inventory,
            variant
        );
    }


    // =====================================================
    // DECREASE
    // =====================================================

    public async Task<InventoryResponseDTO>
        DecreaseAsync(
            long productVariantId,
            DecreaseInventoryDTO request)
    {
        var variant =
            await GetVariant(
                productVariantId
            );

        var inventory =
            await GetInventoryEntity(
                productVariantId
            );


        // -------------------------------------------------
        // Validate quantity
        // -------------------------------------------------

        if (request.Quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng giảm kho phải lớn hơn 0."
            );
        }


        // -------------------------------------------------
        // Available quantity
        // -------------------------------------------------

        int available =
            inventory.Quantity
            - inventory.ReservedQuantity;


        // -------------------------------------------------
        // Không được giảm quá số lượng có thể bán
        // -------------------------------------------------

        if (request.Quantity > available)
        {
            throw new BadRequestException(
                "Số lượng tồn kho khả dụng không đủ."
            );
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

        await _unitOfWork.SaveChangesAsync();


        return MapToDTO(
            inventory,
            variant
        );
    }


    // =====================================================
    // ADJUST
    // =====================================================

    public async Task<InventoryResponseDTO>
        AdjustAsync(
            long productVariantId,
            AdjustInventoryDTO request)
    {
        var variant =
            await GetVariant(
                productVariantId
            );

        var inventory =
            await GetInventoryEntity(
                productVariantId
            );


        // -------------------------------------------------
        // Không được nhỏ hơn ReservedQuantity
        // -------------------------------------------------

        if (request.Quantity <
            inventory.ReservedQuantity)
        {
            throw new BadRequestException(
                "Quantity không được nhỏ hơn ReservedQuantity."
            );
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

        await _unitOfWork.SaveChangesAsync();


        return MapToDTO(
            inventory,
            variant
        );
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
                "Số lượng Reserve phải lớn hơn 0."
            );
        }


        var inventory =
            await GetInventoryEntity(
                productVariantId
            );


        // -------------------------------------------------
        // Available
        // -------------------------------------------------

        int available =
            inventory.Quantity
            - inventory.ReservedQuantity;


        // -------------------------------------------------
        // Không đủ hàng
        // -------------------------------------------------

        if (quantity > available)
        {
            throw new BadRequestException(
                "Không đủ hàng để Reserve."
            );
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

        await _unitOfWork.SaveChangesAsync();
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
                "Số lượng Release phải lớn hơn 0."
            );
        }


        var inventory =
            await GetInventoryEntity(
                productVariantId
            );


        // -------------------------------------------------
        // Validate ReservedQuantity
        // -------------------------------------------------

        if (quantity >
            inventory.ReservedQuantity)
        {
            throw new BadRequestException(
                "Số lượng Release vượt quá ReservedQuantity."
            );
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

        await _unitOfWork.SaveChangesAsync();
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
                "Số lượng Deduct phải lớn hơn 0."
            );
        }


        var inventory =
            await GetInventoryEntity(
                productVariantId
            );


        // -------------------------------------------------
        // Validate ReservedQuantity
        // -------------------------------------------------

        if (quantity >
            inventory.ReservedQuantity)
        {
            throw new BadRequestException(
                "Số lượng Deduct vượt quá ReservedQuantity."
            );
        }


        // -------------------------------------------------
        // Validate Quantity
        // -------------------------------------------------

        if (quantity >
            inventory.Quantity)
        {
            throw new BadRequestException(
                "Quantity không đủ."
            );
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

        await _unitOfWork.SaveChangesAsync();
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
                    productVariantId
                );

        if (variant == null)
        {
            throw new NotFoundException(
                "ProductVariant không tồn tại."
            );
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
                        x.ProductVariantId
                        == productVariantId
                );

        var inventory =
            inventories.FirstOrDefault();

        if (inventory == null)
        {
            throw new NotFoundException(
                "Inventory không tồn tại."
            );
        }

        return inventory;
    }


    // =====================================================
    // MAPPING
    // =====================================================

    private InventoryResponseDTO MapToDTO(
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
                inventory.Quantity
                - inventory.ReservedQuantity,

            IsActive =
                variant.IsActive,

            CreatedAt =
                inventory.CreatedAt,

            UpdatedAt =
                inventory.UpdatedAt
        };
    }
}