using MiniLogistics.BLL.DTOs.ProductVariant;

namespace MiniLogistics.BLL.Services;

public interface IProductVariantService
{
    Task<IEnumerable<ProductVariantResponseDTO>> GetAllAsync();

    Task<ProductVariantResponseDTO?> GetByIdAsync(
        long id);

    Task<IEnumerable<ProductVariantResponseDTO>>
        GetByProductIdAsync(
            long productId);

    Task<ProductVariantResponseDTO>
        CreateAsync(
            long userId,
            CreateProductVariantDTO request);

    Task<ProductVariantResponseDTO?>
        UpdateAsync(
            long userId,
            long id,
            UpdateProductVariantDTO request);

    Task<bool>
        DeleteAsync(
            long userId,
            long id);
}