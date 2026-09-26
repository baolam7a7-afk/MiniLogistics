using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Product;

namespace MiniLogistics.BLL.Services.Product;

public interface IProductService
{
    // =====================================================
    // PUBLIC
    // =====================================================

    Task<PagedResponseDTO<ProductResponseDTO>> GetAllAsync(
        ProductPaginationRequestDTO request);

    Task<ProductResponseDTO?> GetByIdAsync(
        long id);

    Task<IEnumerable<ProductResponseDTO>> GetByCategoryAsync(
        long categoryId);

    Task<IEnumerable<ProductResponseDTO>> SearchAsync(
        string keyword);


    // =====================================================
    // SELLER
    // =====================================================

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