using MiniLogistics.BLL.DTOs.ProductImage;

namespace MiniLogistics.BLL.Services.ProductImage;

public interface IProductImageService
{
    Task<IEnumerable<ProductImageResponseDTO>> GetByProductIdAsync(
        long productId);

    Task<ProductImageResponseDTO?> GetByIdAsync(
        long id);

    Task<ProductImageResponseDTO> CreateAsync(
        long actorUserId,
        string actorRole,
        CreateProductImageDTO request);

    Task<ProductImageResponseDTO> UpdateAsync(
        long id,
        long actorUserId,
        string actorRole,
        UpdateProductImageDTO request);

    Task DeleteAsync(
        long id,
        long actorUserId,
        string actorRole);
}