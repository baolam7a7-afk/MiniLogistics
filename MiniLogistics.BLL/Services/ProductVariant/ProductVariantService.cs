using MiniLogistics.BLL.DTOs.ProductVariant;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;
using InventoryModel = MiniLogistics.DAL.Models.Inventory;

namespace MiniLogistics.BLL.Services;

public class ProductVariantService : IProductVariantService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductVariantService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // =====================================================

    public async Task<IEnumerable<ProductVariantResponseDTO>> GetAllAsync()
    {
        var variants =
            await _unitOfWork.ProductVariants.GetAllAsync();

        var result =
            new List<ProductVariantResponseDTO>();

        foreach (var variant in variants)
        {
            var inventories =
                await _unitOfWork.Inventories.FindAsync(
                    x => x.ProductVariantId == variant.Id
                );

            var inventory =
                inventories.FirstOrDefault();

            result.Add(
                MapToResponseDTO(
                    variant,
                    inventory
                )
            );
        }

        return result;
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<ProductVariantResponseDTO?> GetByIdAsync(long id)
    {
        var variant =
            await _unitOfWork.ProductVariants.GetByIdAsync(id);

        if (variant == null)
        {
            return null;
        }

        var inventories =
            await _unitOfWork.Inventories.FindAsync(
                x => x.ProductVariantId == variant.Id
            );

        var inventory =
            inventories.FirstOrDefault();

        return MapToResponseDTO(
            variant,
            inventory
        );
    }


    // =====================================================
    // GET BY PRODUCT ID
    // =====================================================

    public async Task<IEnumerable<ProductVariantResponseDTO>>
        GetByProductIdAsync(long productId)
    {
        var variants =
            await _unitOfWork.ProductVariants.FindAsync(
                variant => variant.ProductId == productId
            );

        var result =
            new List<ProductVariantResponseDTO>();

        foreach (var variant in variants)
        {
            var inventories =
                await _unitOfWork.Inventories.FindAsync(
                    x => x.ProductVariantId == variant.Id
                );

            var inventory =
                inventories.FirstOrDefault();

            result.Add(
                MapToResponseDTO(
                    variant,
                    inventory
                )
            );
        }

        return result;
    }


    // =====================================================
    // CREATE
    // =====================================================

    public async Task<ProductVariantResponseDTO> CreateAsync(
        CreateProductVariantDTO request)
    {
        // -------------------------------------------------
        // 1. Kiểm tra Product tồn tại
        // -------------------------------------------------

        var product =
            await _unitOfWork.Products.GetByIdAsync(
                request.ProductId
            );

        if (product == null)
        {
            throw new KeyNotFoundException(
                $"Product với ID {request.ProductId} không tồn tại."
            );
        }


        // -------------------------------------------------
        // 2. Validate Price
        // -------------------------------------------------

        if (request.Price < 0)
        {
            throw new ArgumentException(
                "Price không được nhỏ hơn 0."
            );
        }


        // -------------------------------------------------
        // 3. Tạo ProductVariant
        // -------------------------------------------------

        var variant = new ProductVariant
        {
            ProductId = request.ProductId,

            Sku = request.Sku,

            VariantName = request.VariantName,

            AttributesJson = request.AttributesJson,

            Price = request.Price,

            IsActive = request.IsActive,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = null
        };


        // -------------------------------------------------
        // 4. Tạo Inventory cho ProductVariant
        // -------------------------------------------------

        var inventory = new InventoryModel
        {
            ProductVariant = variant,

            Quantity = 0,

            ReservedQuantity = 0,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = null
        };


        // -------------------------------------------------
        // 5. Add ProductVariant
        // -------------------------------------------------

        await _unitOfWork.ProductVariants.AddAsync(
            variant
        );


        // -------------------------------------------------
        // 6. Add Inventory
        // -------------------------------------------------

        await _unitOfWork.Inventories.AddAsync(
            inventory
        );


        // -------------------------------------------------
        // 7. Save Database
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 8. Trả response
        // -------------------------------------------------

        return MapToResponseDTO(
            variant,
            inventory
        );
    }


    // =====================================================
    // UPDATE
    // =====================================================

    public async Task<ProductVariantResponseDTO?> UpdateAsync(
        long id,
        UpdateProductVariantDTO request)
    {
        // -------------------------------------------------
        // 1. Tìm ProductVariant
        // -------------------------------------------------

        var variant =
            await _unitOfWork.ProductVariants.GetByIdAsync(id);

        if (variant == null)
        {
            return null;
        }


        // -------------------------------------------------
        // 2. Validate Price
        // -------------------------------------------------

        if (request.Price < 0)
        {
            throw new ArgumentException(
                "Price không được nhỏ hơn 0."
            );
        }


        // -------------------------------------------------
        // 3. Update ProductVariant
        // -------------------------------------------------

        variant.Sku = request.Sku;

        variant.VariantName = request.VariantName;

        variant.AttributesJson = request.AttributesJson;

        variant.Price = request.Price;

        variant.IsActive = request.IsActive;

        variant.UpdatedAt = DateTime.UtcNow;


        // -------------------------------------------------
        // 4. Update Repository
        // -------------------------------------------------

        _unitOfWork.ProductVariants.Update(
            variant
        );


        // -------------------------------------------------
        // 5. Save
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 6. Lấy Inventory
        // -------------------------------------------------

        var inventories =
            await _unitOfWork.Inventories.FindAsync(
                x => x.ProductVariantId == variant.Id
            );

        var inventory =
            inventories.FirstOrDefault();


        // -------------------------------------------------
        // 7. Response
        // -------------------------------------------------

        return MapToResponseDTO(
            variant,
            inventory
        );
    }


    // =====================================================
    // DELETE
    // =====================================================

    public async Task<bool> DeleteAsync(long id)
    {
        // -------------------------------------------------
        // 1. Tìm ProductVariant
        // -------------------------------------------------

        var variant =
            await _unitOfWork.ProductVariants.GetByIdAsync(id);

        if (variant == null)
        {
            return false;
        }


        // -------------------------------------------------
        // 2. Tìm Inventory
        // -------------------------------------------------

        var inventories =
            await _unitOfWork.Inventories.FindAsync(
                x => x.ProductVariantId == id
            );

        var inventory =
            inventories.FirstOrDefault();


        // -------------------------------------------------
        // 3. Xóa Inventory trước
        // -------------------------------------------------

        if (inventory != null)
        {
            _unitOfWork.Inventories.Delete(
                inventory
            );
        }


        // -------------------------------------------------
        // 4. Xóa ProductVariant
        // -------------------------------------------------

        _unitOfWork.ProductVariants.Delete(
            variant
        );


        // -------------------------------------------------
        // 5. Save
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();

        return true;
    }


    // =====================================================
    // PRIVATE: MAP DTO
    // =====================================================

    private ProductVariantResponseDTO MapToResponseDTO(
        ProductVariant variant,
        InventoryModel? inventory)
    {
        int quantity =
            inventory?.Quantity ?? 0;

        int reservedQuantity =
            inventory?.ReservedQuantity ?? 0;

        int availableQuantity =
            quantity - reservedQuantity;


        return new ProductVariantResponseDTO
        {
            Id = variant.Id,

            ProductId = variant.ProductId,

            Sku = variant.Sku,

            VariantName = variant.VariantName,

            AttributesJson = variant.AttributesJson,

            Price = variant.Price,

            // Stock = số lượng có thể bán
            // Stock = Quantity - ReservedQuantity
            Stock = availableQuantity,

            IsActive = variant.IsActive,

            CreatedAt = variant.CreatedAt,

            UpdatedAt = variant.UpdatedAt
        };
    }
}