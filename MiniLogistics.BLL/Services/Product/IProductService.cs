using MiniLogistics.BLL.DTOs.Product;

namespace MiniLogistics.BLL.Services.Product;

public interface IProductService
{
    Task<IEnumerable<ProductResponseDTO>> GetAllAsync();

    Task<ProductResponseDTO?> GetByIdAsync(long id);

    Task<IEnumerable<ProductResponseDTO>> GetByCategoryAsync(long categoryId);

    Task<IEnumerable<ProductResponseDTO>> SearchAsync(string keyword);

    Task<ProductResponseDTO> CreateAsync(CreateProductDTO request);

    Task<ProductResponseDTO?> UpdateAsync(
        long id,
        UpdateProductDTO request);

    Task<bool> DeleteAsync(long id);
}