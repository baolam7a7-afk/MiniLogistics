using MiniLogistics.BLL.DTOs.ProductVariant;

namespace MiniLogistics.BLL.Services;

public interface IProductVariantService
{
    Task<IEnumerable<ProductVariantResponseDTO>> GetAllAsync();

    Task<ProductVariantResponseDTO?> GetByIdAsync(long id);

    Task<IEnumerable<ProductVariantResponseDTO>> GetByProductIdAsync(
        long productId
    );

    Task<ProductVariantResponseDTO> CreateAsync(
        CreateProductVariantDTO request
    );

    Task<ProductVariantResponseDTO?> UpdateAsync(
        long id,
        UpdateProductVariantDTO request
    );

    Task<bool> DeleteAsync(long id);
}