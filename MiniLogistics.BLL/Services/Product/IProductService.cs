using MiniLogistics.BLL.DTOs.Product;

namespace MiniLogistics.BLL.Services;

public interface IProductService
{
    // ========================================
    // GET ALL
    // ========================================

    Task<IEnumerable<ProductResponseDTO>> GetAllAsync();


    // ========================================
    // GET BY ID
    // ========================================

    Task<ProductResponseDTO?> GetByIdAsync(long id);


    // ========================================
    // CREATE
    // ========================================

    Task<ProductResponseDTO> CreateAsync(
        CreateProductDTO request
    );


    // ========================================
    // UPDATE
    // ========================================

    Task<ProductResponseDTO?> UpdateAsync(
        long id,
        UpdateProductDTO request
    );


    // ========================================
    // DELETE
    // ========================================

    Task<bool> DeleteAsync(long id);
}