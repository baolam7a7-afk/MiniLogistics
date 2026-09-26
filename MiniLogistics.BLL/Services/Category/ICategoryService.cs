using MiniLogistics.BLL.DTOs.Category;
using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.Services;

public interface ICategoryService
{
    // =====================================================
    // GET ALL
    // =====================================================

    Task<PagedResponseDTO<CategoryResponseDTO>> GetAllAsync(
        CategoryPaginationRequestDTO request);


    // =====================================================
    // GET BY ID
    // =====================================================

    Task<CategoryResponseDTO?> GetByIdAsync(
        long id);


    // =====================================================
    // CREATE
    // =====================================================

    Task<CategoryResponseDTO> CreateAsync(
        CreateCategoryDTO request);


    // =====================================================
    // UPDATE
    // =====================================================

    Task<CategoryResponseDTO?> UpdateAsync(
        long id,
        UpdateCategoryDTO request);


    // =====================================================
    // DELETE
    // =====================================================

    Task<bool> DeleteAsync(
        long id);
}