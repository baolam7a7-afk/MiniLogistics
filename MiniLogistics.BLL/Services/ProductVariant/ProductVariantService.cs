using MiniLogistics.BLL.DTOs.ProductVariant;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

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

        return variants.Select(variant => new ProductVariantResponseDTO
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Sku = variant.Sku,
            VariantName = variant.VariantName,
            AttributesJson = variant.AttributesJson,
            Price = variant.Price,
            Stock = variant.Stock,
            IsActive = variant.IsActive,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        });
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

        return new ProductVariantResponseDTO
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Sku = variant.Sku,
            VariantName = variant.VariantName,
            AttributesJson = variant.AttributesJson,
            Price = variant.Price,
            Stock = variant.Stock,
            IsActive = variant.IsActive,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        };
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

        return variants.Select(variant => new ProductVariantResponseDTO
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Sku = variant.Sku,
            VariantName = variant.VariantName,
            AttributesJson = variant.AttributesJson,
            Price = variant.Price,
            Stock = variant.Stock,
            IsActive = variant.IsActive,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        });
    }


    // =====================================================
    // CREATE
    // =====================================================

    public async Task<ProductVariantResponseDTO> CreateAsync(
        CreateProductVariantDTO request)
    {
        // Kiểm tra Product tồn tại
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


        // Validate Price
        if (request.Price < 0)
        {
            throw new ArgumentException(
                "Price không được nhỏ hơn 0."
            );
        }


        // Validate Stock
        if (request.Stock < 0)
        {
            throw new ArgumentException(
                "Stock không được nhỏ hơn 0."
            );
        }


        // Tạo entity
        var variant = new ProductVariant
        {
            ProductId = request.ProductId,

            Sku = request.Sku,

            VariantName = request.VariantName,

            AttributesJson = request.AttributesJson,

            Price = request.Price,

            Stock = request.Stock,

            IsActive = request.IsActive,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = null
        };


        // Add vào Repository
        await _unitOfWork.ProductVariants.AddAsync(variant);


        // Save database
        await _unitOfWork.SaveChangesAsync();


        // Trả response
        return new ProductVariantResponseDTO
        {
            Id = variant.Id,

            ProductId = variant.ProductId,

            Sku = variant.Sku,

            VariantName = variant.VariantName,

            AttributesJson = variant.AttributesJson,

            Price = variant.Price,

            Stock = variant.Stock,

            IsActive = variant.IsActive,

            CreatedAt = variant.CreatedAt,

            UpdatedAt = variant.UpdatedAt
        };
    }


    // =====================================================
    // UPDATE
    // =====================================================

    public async Task<ProductVariantResponseDTO?> UpdateAsync(
        long id,
        UpdateProductVariantDTO request)
    {
        var variant =
            await _unitOfWork.ProductVariants.GetByIdAsync(id);

        if (variant == null)
        {
            return null;
        }


        // Validate Price
        if (request.Price < 0)
        {
            throw new ArgumentException(
                "Price không được nhỏ hơn 0."
            );
        }


        // Validate Stock
        if (request.Stock < 0)
        {
            throw new ArgumentException(
                "Stock không được nhỏ hơn 0."
            );
        }


        // Update
        variant.Sku = request.Sku;

        variant.VariantName = request.VariantName;

        variant.AttributesJson = request.AttributesJson;

        variant.Price = request.Price;

        variant.Stock = request.Stock;

        variant.IsActive = request.IsActive;

        variant.UpdatedAt = DateTime.UtcNow;


        // Update Repository
        _unitOfWork.ProductVariants.Update(variant);


        // Save
        await _unitOfWork.SaveChangesAsync();


        // Response
        return new ProductVariantResponseDTO
        {
            Id = variant.Id,

            ProductId = variant.ProductId,

            Sku = variant.Sku,

            VariantName = variant.VariantName,

            AttributesJson = variant.AttributesJson,

            Price = variant.Price,

            Stock = variant.Stock,

            IsActive = variant.IsActive,

            CreatedAt = variant.CreatedAt,

            UpdatedAt = variant.UpdatedAt
        };
    }


    // =====================================================
    // DELETE
    // =====================================================

    public async Task<bool> DeleteAsync(long id)
    {
        var variant =
            await _unitOfWork.ProductVariants.GetByIdAsync(id);

        if (variant == null)
        {
            return false;
        }


        _unitOfWork.ProductVariants.Delete(variant);

        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}