using MiniLogistics.BLL.DTOs.Product;

namespace MiniLogistics.BLL.Services.Product;

public interface IProductService
{
    Task<IEnumerable<ProductResponseDTO>> GetAllAsync();

    Task<ProductResponseDTO?> GetByIdAsync(long id);

    Task<IEnumerable<ProductResponseDTO>> GetByCategoryAsync(
        long categoryId);

    Task<IEnumerable<ProductResponseDTO>> SearchAsync(
        string keyword);

    Task<ProductResponseDTO> CreateAsync(
        long userId,
        CreateProductDTO request);

    Task<ProductResponseDTO?> UpdateAsync(
        long userId,
        long id,
        UpdateProductDTO request);

    Task<bool> DeleteAsync(
        long userId,
        long id);
}