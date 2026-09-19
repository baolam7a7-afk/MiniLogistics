using MiniLogistics.BLL.DTOs.Product;

using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;


namespace MiniLogistics.BLL.Services;

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


        return products.Select(product =>
            new ProductResponseDTO
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
            }
        );
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


    // =====================================================
    // CREATE
    // =====================================================

    public async Task<ProductResponseDTO> CreateAsync(
        CreateProductDTO request)
    {
        var product = new Product
        {
            ShopId = request.ShopId,

            CategoryId = request.CategoryId,

            Name = request.Name,

            Slug = request.Slug,

            Description = request.Description,

            Status = request.Status,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = null
        };


        await _unitOfWork.Products.AddAsync(product);


        await _unitOfWork.SaveChangesAsync();


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


    // =====================================================
    // UPDATE
    // =====================================================

    public async Task<ProductResponseDTO?> UpdateAsync(
        long id,
        UpdateProductDTO request)
    {
        var product =
            await _unitOfWork.Products.GetByIdAsync(id);


        if (product == null)
        {
            return null;
        }


        product.ShopId =
            request.ShopId;

        product.CategoryId =
            request.CategoryId;

        product.Name =
            request.Name;

        product.Slug =
            request.Slug;

        product.Description =
            request.Description;

        product.Status =
            request.Status;

        product.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Products.Update(product);


        await _unitOfWork.SaveChangesAsync();


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


    // =====================================================
    // DELETE
    // =====================================================

    public async Task<bool> DeleteAsync(long id)
    {
        var product =
            await _unitOfWork.Products.GetByIdAsync(id);


        if (product == null)
        {
            return false;
        }


        _unitOfWork.Products.Delete(product);


        await _unitOfWork.SaveChangesAsync();


        return true;
    }
}