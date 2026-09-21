using MiniLogistics.BLL.DTOs.Category;

namespace MiniLogistics.BLL.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDTO>> GetAllAsync();

    Task<CategoryResponseDTO?> GetByIdAsync(long id);

    Task<CategoryResponseDTO> CreateAsync(
        CreateCategoryDTO request
    );

    Task<CategoryResponseDTO?> UpdateAsync(
        long id,
        UpdateCategoryDTO request
    );

    Task<bool> DeleteAsync(long id);
}