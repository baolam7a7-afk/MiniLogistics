using MiniLogistics.BLL.DTOs.ProductImage;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

using ProductModel = MiniLogistics.DAL.Models.Product;
using ProductImageModel = MiniLogistics.DAL.Models.ProductImage;

namespace MiniLogistics.BLL.Services.ProductImage;

public class ProductImageService : IProductImageService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductImageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // GET BY PRODUCT
    // =====================================================

    public async Task<IEnumerable<ProductImageResponseDTO>>
        GetByProductIdAsync(long productId)
    {
        var product =
            await _unitOfWork.Products
                .GetByIdAsync(productId);

        if (product == null)
        {
            throw new NotFoundException(
                "Product không tồn tại.");
        }

        var images =
            await _unitOfWork.ProductImages
                .FindAsync(x =>
                    x.ProductId == productId);

        return images
            .OrderBy(x => x.Id)
            .Select(MapToResponse);
    }

    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<ProductImageResponseDTO?>
        GetByIdAsync(long id)
    {
        var image =
            await _unitOfWork.ProductImages
                .GetByIdAsync(id);

        if (image == null)
        {
            return null;
        }

        return MapToResponse(image);
    }

    // =====================================================
    // CREATE
    // =====================================================

    public async Task<ProductImageResponseDTO>
        CreateAsync(
            long actorUserId,
            string actorRole,
            CreateProductImageDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new BadRequestException(
                "Url không được để trống.");
        }

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(request.ProductId);

        if (product == null)
        {
            throw new NotFoundException(
                "Product không tồn tại.");
        }

        await CheckProductAccess(
            product,
            actorUserId,
            actorRole);

        var image = new ProductImageModel
        {
            ProductId = product.Id,
            Url = request.Url.Trim()
        };

        await _unitOfWork.ProductImages
            .AddAsync(image);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(image);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    public async Task<ProductImageResponseDTO>
        UpdateAsync(
            long id,
            long actorUserId,
            string actorRole,
            UpdateProductImageDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new BadRequestException(
                "Url không được để trống.");
        }

        var image =
            await _unitOfWork.ProductImages
                .GetByIdAsync(id);

        if (image == null)
        {
            throw new NotFoundException(
                "ProductImage không tồn tại.");
        }

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(image.ProductId);

        if (product == null)
        {
            throw new NotFoundException(
                "Product của Image không tồn tại.");
        }

        await CheckProductAccess(
            product,
            actorUserId,
            actorRole);

        image.Url = request.Url.Trim();

        _unitOfWork.ProductImages
            .Update(image);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(image);
    }

    // =====================================================
    // DELETE
    // =====================================================

    public async Task DeleteAsync(
        long id,
        long actorUserId,
        string actorRole)
    {
        var image =
            await _unitOfWork.ProductImages
                .GetByIdAsync(id);

        if (image == null)
        {
            throw new NotFoundException(
                "ProductImage không tồn tại.");
        }

        var product =
            await _unitOfWork.Products
                .GetByIdAsync(image.ProductId);

        if (product == null)
        {
            throw new NotFoundException(
                "Product của Image không tồn tại.");
        }

        await CheckProductAccess(
            product,
            actorUserId,
            actorRole);

        _unitOfWork.ProductImages
            .Delete(image);

        await _unitOfWork.SaveChangesAsync();
    }

    // =====================================================
    // CHECK PRODUCT ACCESS
    // =====================================================

    private async Task CheckProductAccess(
        ProductModel  product,
        long actorUserId,
        string actorRole)
    {
        actorRole =
            actorRole.Trim().ToLowerInvariant();

        if (actorRole == "admin")
        {
            return;
        }

        if (actorRole != "seller")
        {
            throw new ForbiddenException(
                "Bạn không có quyền quản lý ProductImage.");
        }

        bool ownsShop =
            await _unitOfWork.Shops.AnyAsync(x =>
                x.Id == product.ShopId &&
                x.OwnerUserId == actorUserId);

        if (!ownsShop)
        {
            throw new ForbiddenException(
                "Bạn không có quyền quản lý Image của Product này.");
        }
    }

    // =====================================================
    // MAP
    // =====================================================

    private ProductImageResponseDTO
        MapToResponse(ProductImageModel   image)
    {
        return new ProductImageResponseDTO
        {
            Id = image.Id,
            ProductId = image.ProductId,
            Url = image.Url
        };
    }
}