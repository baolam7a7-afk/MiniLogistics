using System.Linq.Expressions;

using MiniLogistics.BLL.DTOs.Category;
using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.Exceptions;

using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL
    // PAGINATION + SEARCH + FILTER
    // =====================================================

    public async Task<PagedResponseDTO<CategoryResponseDTO>>
        GetAllAsync(
            CategoryPaginationRequestDTO request)
    {
        // =================================================
        // VALIDATE REQUEST
        // =================================================

        if (request == null)
        {
            request =
                new CategoryPaginationRequestDTO();
        }


        // =================================================
        // VALIDATE PAGINATION
        // =================================================

        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }


        // =================================================
        // PREPARE FILTER
        // =================================================

        var search =
            request.Search?
                .Trim()
                .ToLower();


        Expression<Func<Category, bool>>? predicate =
            null;


        // =================================================
        // SEARCH + ACTIVE + PARENT
        // =================================================

        predicate =
            category =>
                (
                    string.IsNullOrWhiteSpace(search)
                    ||
                    category.Name
                        .ToLower()
                        .Contains(search)
                    ||
                    category.Slug
                        .ToLower()
                        .Contains(search)
                )
                &&
                (
                    !request.IsActive.HasValue
                    ||
                    category.IsActive ==
                        request.IsActive.Value
                )
                &&
                (
                    !request.ParentId.HasValue
                    ||
                    category.ParentId ==
                        request.ParentId.Value
                );


        // =================================================
        // QUERY DATABASE
        // =================================================

        var result =
            await _unitOfWork.Categories
                .GetPagedAsync(
                    request.Page,
                    request.PageSize,
                    predicate,
                    query =>
                        query.OrderByDescending(
                            x => x.Id));


        // =================================================
        // MAP RESULT
        // =================================================

        var items =
            result.Items
                .Select(MapToResponseDTO)
                .ToList();


        // =================================================
        // CALCULATE TOTAL PAGES
        // =================================================

        var totalPages =
            result.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    result.TotalItems /
                    (double)request.PageSize);


        // =================================================
        // RETURN
        // =================================================

        return new PagedResponseDTO<CategoryResponseDTO>
        {
            Items =
                items,

            Page =
                request.Page,

            PageSize =
                request.PageSize,

            TotalItems =
                result.TotalItems,

            TotalPages =
                totalPages
        };
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<CategoryResponseDTO?>
        GetByIdAsync(
            long id)
    {
        var category =
            await _unitOfWork.Categories
                .GetByIdAsync(id);

        if (category == null)
        {
            return null;
        }

        return MapToResponseDTO(category);
    }


    // =====================================================
    // CREATE
    // ADMIN ONLY
    // =====================================================

    public async Task<CategoryResponseDTO>
        CreateAsync(
            CreateCategoryDTO request)
    {
        // =================================================
        // VALIDATE REQUEST
        // =================================================

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }


        // =================================================
        // VALIDATE NAME
        // =================================================

        if (string.IsNullOrWhiteSpace(
                request.Name))
        {
            throw new BadRequestException(
                "Tên category không được để trống.");
        }


        // =================================================
        // VALIDATE SLUG
        // =================================================

        if (string.IsNullOrWhiteSpace(
                request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống.");
        }


        var name =
            request.Name.Trim();

        var slug =
            request.Slug
                .Trim()
                .ToLower();


        // =================================================
        // CHECK PARENT
        // =================================================

        if (request.ParentId.HasValue)
        {
            var parent =
                await _unitOfWork.Categories
                    .GetByIdAsync(
                        request.ParentId.Value);

            if (parent == null)
            {
                throw new NotFoundException(
                    $"Category cha với ID {request.ParentId.Value} không tồn tại.");
            }
        }


        // =================================================
        // CHECK DUPLICATE SLUG
        // =================================================

        var slugExists =
            await _unitOfWork.Categories
                .AnyAsync(
                    c => c.Slug == slug);

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{slug}' đã tồn tại.");
        }


        // =================================================
        // CREATE ENTITY
        // =================================================

        var category =
            new Category
            {
                ParentId =
                    request.ParentId,

                Name =
                    name,

                Slug =
                    slug,

                IsActive =
                    request.IsActive,

                CreatedAt =
                    DateTime.UtcNow
            };


        // =================================================
        // ADD
        // =================================================

        await _unitOfWork.Categories
            .AddAsync(category);


        // =================================================
        // SAVE
        // =================================================

        await _unitOfWork
            .SaveChangesAsync();


        // =================================================
        // RESPONSE
        // =================================================

        return MapToResponseDTO(category);
    }


    // =====================================================
    // UPDATE
    // ADMIN ONLY
    // =====================================================

    public async Task<CategoryResponseDTO?>
        UpdateAsync(
            long id,
            UpdateCategoryDTO request)
    {
        // =================================================
        // VALIDATE REQUEST
        // =================================================

        if (request == null)
        {
            throw new BadRequestException(
                "Request không được null.");
        }


        // =================================================
        // FIND CATEGORY
        // =================================================

        var category =
            await _unitOfWork.Categories
                .GetByIdAsync(id);

        if (category == null)
        {
            return null;
        }


        // =================================================
        // VALIDATE NAME
        // =================================================

        if (string.IsNullOrWhiteSpace(
                request.Name))
        {
            throw new BadRequestException(
                "Tên category không được để trống.");
        }


        // =================================================
        // VALIDATE SLUG
        // =================================================

        if (string.IsNullOrWhiteSpace(
                request.Slug))
        {
            throw new BadRequestException(
                "Slug không được để trống.");
        }


        var name =
            request.Name.Trim();

        var slug =
            request.Slug
                .Trim()
                .ToLower();


        // =================================================
        // PREVENT SELF PARENT
        // =================================================

        if (request.ParentId == id)
        {
            throw new BadRequestException(
                "Category không thể làm Parent của chính nó.");
        }


        // =================================================
        // CHECK PARENT
        // =================================================

        if (request.ParentId.HasValue)
        {
            var parent =
                await _unitOfWork.Categories
                    .GetByIdAsync(
                        request.ParentId.Value);

            if (parent == null)
            {
                throw new NotFoundException(
                    $"Category cha với ID {request.ParentId.Value} không tồn tại.");
            }
        }


        // =================================================
        // CHECK DUPLICATE SLUG
        // =================================================

        var slugExists =
            await _unitOfWork.Categories
                .AnyAsync(
                    c =>
                        c.Slug == slug &&
                        c.Id != id);

        if (slugExists)
        {
            throw new BadRequestException(
                $"Slug '{slug}' đã tồn tại.");
        }


        // =================================================
        // UPDATE ENTITY
        // =================================================

        category.ParentId =
            request.ParentId;

        category.Name =
            name;

        category.Slug =
            slug;

        category.IsActive =
            request.IsActive;


        // =================================================
        // UPDATE REPOSITORY
        // =================================================

        _unitOfWork.Categories
            .Update(category);


        // =================================================
        // SAVE
        // =================================================

        await _unitOfWork
            .SaveChangesAsync();


        // =================================================
        // RESPONSE
        // =================================================

        return MapToResponseDTO(category);
    }


    // =====================================================
    // DELETE
    // ADMIN ONLY
    // =====================================================

    public async Task<bool>
        DeleteAsync(
            long id)
    {
        // =================================================
        // FIND CATEGORY
        // =================================================

        var category =
            await _unitOfWork.Categories
                .GetByIdAsync(id);

        if (category == null)
        {
            return false;
        }


        // =================================================
        // CHECK CATEGORY CHILDREN
        // =================================================

        var hasChildren =
            await _unitOfWork.Categories
                .AnyAsync(
                    c => c.ParentId == id);

        if (hasChildren)
        {
            throw new BadRequestException(
                "Không thể xóa Category đang có Category con.");
        }


        // =================================================
        // CHECK PRODUCTS
        // =================================================

        var hasProducts =
            await _unitOfWork.Products
                .AnyAsync(
                    p => p.CategoryId == id);

        if (hasProducts)
        {
            throw new BadRequestException(
                "Không thể xóa Category đang được Product sử dụng.");
        }


        // =================================================
        // DELETE
        // =================================================

        _unitOfWork.Categories
            .Delete(category);


        // =================================================
        // SAVE
        // =================================================

        await _unitOfWork
            .SaveChangesAsync();


        return true;
    }


    // =====================================================
    // MAP
    // =====================================================

    private static CategoryResponseDTO
        MapToResponseDTO(
            Category category)
    {
        return new CategoryResponseDTO
        {
            Id =
                category.Id,

            ParentId =
                category.ParentId,

            Name =
                category.Name,

            Slug =
                category.Slug,

            IsActive =
                category.IsActive,

            CreatedAt =
                category.CreatedAt
        };
    }
}