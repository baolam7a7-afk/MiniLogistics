
using MiniLogistics.BLL.DTOs.Category;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // =====================================================

    public async Task<IEnumerable<CategoryResponseDTO>>
        GetAllAsync()
    {
        var categories =
            await _unitOfWork.Categories.GetAllAsync();

        return categories.Select(category =>
            new CategoryResponseDTO
            {
                Id = category.Id,

                ParentId = category.ParentId,

                Name = category.Name,

                Slug = category.Slug,

                IsActive = category.IsActive,

                CreatedAt = category.CreatedAt
            });
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<CategoryResponseDTO?>
        GetByIdAsync(long id)
    {
        var category =
            await _unitOfWork.Categories.GetByIdAsync(id);

        if (category == null)
        {
            return null;
        }

        return new CategoryResponseDTO
        {
            Id = category.Id,

            ParentId = category.ParentId,

            Name = category.Name,

            Slug = category.Slug,

            IsActive = category.IsActive,

            CreatedAt = category.CreatedAt
        };
    }


    // =====================================================
    // CREATE
    // =====================================================

    public async Task<CategoryResponseDTO>
        CreateAsync(CreateCategoryDTO request)
    {
        // -------------------------------------------------
        // 1. Validate Name
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Tên category không được để trống."
            );
        }


        // -------------------------------------------------
        // 2. Validate Slug
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new ArgumentException(
                "Slug không được để trống."
            );
        }


        // -------------------------------------------------
        // 3. Check Parent
        // -------------------------------------------------

        if (request.ParentId.HasValue)
        {
            var parent =
                await _unitOfWork.Categories.GetByIdAsync(
                    request.ParentId.Value
                );

            if (parent == null)
            {
                throw new NotFoundException(
                    $"Category cha với ID {request.ParentId.Value} không tồn tại."
                );
            }
        }


        // -------------------------------------------------
        // 4. Check duplicate slug
        // -------------------------------------------------

        var slugExists =
            await _unitOfWork.Categories.AnyAsync(
                c => c.Slug == request.Slug
            );

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{request.Slug}' đã tồn tại."
            );
        }


        // -------------------------------------------------
        // 5. Create Entity
        // -------------------------------------------------

        var category = new Category
        {
            ParentId = request.ParentId,

            Name = request.Name.Trim(),

            Slug = request.Slug.Trim().ToLower(),

            IsActive = request.IsActive,

            CreatedAt = DateTime.UtcNow
        };


        // -------------------------------------------------
        // 6. Add
        // -------------------------------------------------

        await _unitOfWork.Categories.AddAsync(category);


        // -------------------------------------------------
        // 7. Save
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 8. Response
        // -------------------------------------------------

        return new CategoryResponseDTO
        {
            Id = category.Id,

            ParentId = category.ParentId,

            Name = category.Name,

            Slug = category.Slug,

            IsActive = category.IsActive,

            CreatedAt = category.CreatedAt
        };
    }


    // =====================================================
    // UPDATE
    // =====================================================

    public async Task<CategoryResponseDTO?>
        UpdateAsync(
            long id,
            UpdateCategoryDTO request)
    {
        // -------------------------------------------------
        // 1. Find category
        // -------------------------------------------------

        var category =
            await _unitOfWork.Categories.GetByIdAsync(id);

        if (category == null)
        {
            return null;
        }


        // -------------------------------------------------
        // 2. Validate Name
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Tên category không được để trống."
            );
        }


        // -------------------------------------------------
        // 3. Validate Slug
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new ArgumentException(
                "Slug không được để trống."
            );
        }


        // -------------------------------------------------
        // 4. Prevent category becoming itself
        // -------------------------------------------------

        if (request.ParentId == id)
        {
            throw new BadRequestException(
                "Category không thể làm Parent của chính nó."
            );
        }


        // -------------------------------------------------
        // 5. Check Parent
        // -------------------------------------------------

        if (request.ParentId.HasValue)
        {
            var parent =
                await _unitOfWork.Categories.GetByIdAsync(
                    request.ParentId.Value
                );

            if (parent == null)
            {
                throw new NotFoundException(
                    $"Category cha với ID {request.ParentId.Value} không tồn tại."
                );
            }
        }


        // -------------------------------------------------
        // 6. Check duplicate slug
        // -------------------------------------------------

        var slugExists =
            await _unitOfWork.Categories.AnyAsync(
                c =>
                    c.Slug == request.Slug &&
                    c.Id != id
            );

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{request.Slug}' đã tồn tại."
            );
        }


        // -------------------------------------------------
        // 7. Update
        // -------------------------------------------------

        category.ParentId =
            request.ParentId;

        category.Name =
            request.Name.Trim();

        category.Slug =
            request.Slug.Trim().ToLower();

        category.IsActive =
            request.IsActive;


        // -------------------------------------------------
        // 8. Update Repository
        // -------------------------------------------------

        _unitOfWork.Categories.Update(category);


        // -------------------------------------------------
        // 9. Save
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 10. Response
        // -------------------------------------------------

        return new CategoryResponseDTO
        {
            Id = category.Id,

            ParentId = category.ParentId,

            Name = category.Name,

            Slug = category.Slug,

            IsActive = category.IsActive,

            CreatedAt = category.CreatedAt
        };
    }


    // =====================================================
    // DELETE
    // =====================================================

   public async Task<bool> DeleteAsync(long id)
{
    var category = await _unitOfWork.Categories.GetByIdAsync(id);

    if (category == null)
    {
        return false;
    }

    // 1. Kiểm tra Category con
    var hasChildren = await _unitOfWork.Categories
        .AnyAsync(c => c.ParentId == id);

    if (hasChildren)
    {
        throw new BadRequestException(
            "Không thể xóa Category đang có Category con."
        );
    }

    // 2. Kiểm tra Product đang sử dụng Category
    var hasProducts = await _unitOfWork.Products
        .AnyAsync(p => p.CategoryId == id);

    if (hasProducts)
    {
        throw new BadRequestException(
            "Không thể xóa Category đang được Product sử dụng."
        );
    }

    // 3. Xóa Category
    _unitOfWork.Categories.Delete(category);

    await _unitOfWork.SaveChangesAsync();

    return true;
}
}