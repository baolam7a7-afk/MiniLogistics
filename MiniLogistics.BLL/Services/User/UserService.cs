using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.User;
using MiniLogistics.BLL.Exceptions;

using UserModel = MiniLogistics.DAL.Models.User;
using UserRoleModel = MiniLogistics.DAL.Models.UserRole;

using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services.User;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;


    // =====================================================
    // ALLOWED ROLES
    // =====================================================

    private static readonly HashSet<string> AllowedRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "customer",
            "seller",
            "shipper",
            "admin"
        };


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public UserService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =====================================================
    // GET ALL USERS - PAGINATION
    // =====================================================

    public async Task<PagedResponseDTO<UserResponseDTO>>
        GetAllAsync(
            UserPaginationRequestDTO request)
    {
        // -------------------------------------------------
        // VALIDATE REQUEST
        // -------------------------------------------------

        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }


        // -------------------------------------------------
        // VALIDATE PAGE
        // -------------------------------------------------

        if (request.Page < 1)
        {
            throw new BadRequestException(
                "Page phải lớn hơn hoặc bằng 1.");
        }


        // -------------------------------------------------
        // VALIDATE PAGE SIZE
        // -------------------------------------------------

        if (request.PageSize < 1)
        {
            throw new BadRequestException(
                "PageSize phải lớn hơn hoặc bằng 1.");
        }


        // -------------------------------------------------
        // GIỚI HẠN PAGE SIZE
        // -------------------------------------------------

        if (request.PageSize > 100)
        {
            throw new BadRequestException(
                "PageSize không được lớn hơn 100.");
        }


        // -------------------------------------------------
        // NORMALIZE SEARCH
        // -------------------------------------------------

        string? search =
            string.IsNullOrWhiteSpace(request.Search)
                ? null
                : request.Search.Trim();


        // -------------------------------------------------
        // NORMALIZE ROLE
        // -------------------------------------------------

        string? role =
            string.IsNullOrWhiteSpace(request.Role)
                ? null
                : request.Role
                    .Trim()
                    .ToLowerInvariant();


        // -------------------------------------------------
        // NORMALIZE STATUS
        // -------------------------------------------------

        string? status =
            string.IsNullOrWhiteSpace(request.Status)
                ? null
                : request.Status
                    .Trim()
                    .ToLowerInvariant();


        // -------------------------------------------------
        // ROLE USER IDS
        // -------------------------------------------------

        HashSet<long>? roleUserIds = null;


        if (!string.IsNullOrWhiteSpace(role))
        {
            // ---------------------------------------------
            // Tìm Role
            // ---------------------------------------------

            var roles =
                await _unitOfWork.Roles
                    .FindAsync(
                        x =>
                            x.Name != null
                            &&
                            x.Name.ToLower() == role);


            var selectedRole =
                roles.FirstOrDefault();


            // ---------------------------------------------
            // Role không tồn tại
            // ---------------------------------------------

            if (selectedRole == null)
            {
                return new PagedResponseDTO<UserResponseDTO>
                {
                    Items = new List<UserResponseDTO>(),

                    Page = request.Page,

                    PageSize = request.PageSize,

                    TotalItems = 0,

                    TotalPages = 0
                };
            }


            // ---------------------------------------------
            // Lấy UserId thuộc Role
            // ---------------------------------------------

            var userRoles =
                await _unitOfWork.UserRoles
                    .FindAsync(
                        x =>
                            x.RoleId
                            == selectedRole.Id);


            roleUserIds =
                userRoles
                    .Select(x => x.UserId)
                    .ToHashSet();
        }


        // =================================================
        // PAGINATION QUERY
        // =================================================

        var pagedUsers =
            await _unitOfWork.Users
                .GetPagedAsync(
                    request.Page,
                    request.PageSize,

                    user =>
                        (
                            // ---------------------------------
                            // SEARCH
                            // ---------------------------------

                            string.IsNullOrWhiteSpace(search)
                            ||
                            user.Email.Contains(search)
                            ||
                            user.FullName.Contains(search)
                            ||
                            (
                                user.Phone != null
                                &&
                                user.Phone.Contains(search)
                            )
                        )

                        &&

                        (
                            // ---------------------------------
                            // STATUS
                            // ---------------------------------

                            string.IsNullOrWhiteSpace(status)
                            ||
                            user.Status == status
                        )

                        &&

                        (
                            // ---------------------------------
                            // ROLE
                            // ---------------------------------

                            roleUserIds == null
                            ||
                            roleUserIds.Contains(user.Id)
                        ),

                    // -----------------------------------------
                    // ORDER BY
                    // -----------------------------------------

                    query =>
                        query.OrderBy(
                            user => user.Id)
                );


        // =================================================
        // MAP ENTITY -> DTO
        // =================================================

        var result =
            new List<UserResponseDTO>();


        foreach (var user in pagedUsers.Items)
        {
            result.Add(
                await MapToResponseAsync(user));
        }


        // =================================================
        // CALCULATE TOTAL PAGES
        // =================================================

        int totalPages =
            pagedUsers.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    pagedUsers.TotalItems
                    / (double)request.PageSize);


        // =================================================
        // RETURN
        // =================================================

        return new PagedResponseDTO<UserResponseDTO>
        {
            Items = result,

            Page = request.Page,

            PageSize = request.PageSize,

            TotalItems = pagedUsers.TotalItems,

            TotalPages = totalPages
        };
    }


    // =====================================================
    // GET USER BY ID
    // =====================================================

    public async Task<UserResponseDTO?> GetByIdAsync(
        long userId)
    {
        if (userId <= 0)
        {
            throw new BadRequestException(
                "UserId không hợp lệ.");
        }


        var user =
            await _unitOfWork.Users
                .GetByIdAsync(userId);


        if (user == null)
        {
            return null;
        }


        return await MapToResponseAsync(user);
    }


    // =====================================================
    // LOCK USER
    // =====================================================

    public async Task<UserResponseDTO> LockAsync(
        long userId)
    {
        if (userId <= 0)
        {
            throw new BadRequestException(
                "UserId không hợp lệ.");
        }


        var user =
            await _unitOfWork.Users
                .GetByIdAsync(userId);


        if (user == null)
        {
            throw new NotFoundException(
                $"User {userId} không tồn tại.");
        }


        // -------------------------------------------------
        // Lấy role hiện tại
        // -------------------------------------------------

        var roles =
            await GetRoleNamesAsync(user.Id);


        // -------------------------------------------------
        // Không cho khóa Admin
        // -------------------------------------------------

        if (roles.Any(
            x =>
                string.Equals(
                    x,
                    "admin",
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException(
                "Không thể khóa tài khoản Admin.");
        }


        // -------------------------------------------------
        // LOCK
        // -------------------------------------------------

        user.Status = "locked";

        user.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Users
            .Update(user);


        await _unitOfWork
            .SaveChangesAsync();


        return await MapToResponseAsync(user);
    }


    // =====================================================
    // UNLOCK USER
    // =====================================================

    public async Task<UserResponseDTO> UnlockAsync(
        long userId)
    {
        if (userId <= 0)
        {
            throw new BadRequestException(
                "UserId không hợp lệ.");
        }


        var user =
            await _unitOfWork.Users
                .GetByIdAsync(userId);


        if (user == null)
        {
            throw new NotFoundException(
                $"User {userId} không tồn tại.");
        }


        // -------------------------------------------------
        // UNLOCK
        // -------------------------------------------------

        user.Status = "active";

        user.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Users
            .Update(user);


        await _unitOfWork
            .SaveChangesAsync();


        return await MapToResponseAsync(user);
    }


    // =====================================================
    // UPDATE ROLE
    // =====================================================

    public async Task<UserResponseDTO> UpdateRoleAsync(
        long userId,
        UpdateUserRoleDTO request)
    {
        if (userId <= 0)
        {
            throw new BadRequestException(
                "UserId không hợp lệ.");
        }


        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }


        // -------------------------------------------------
        // Validate Role
        // -------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            throw new BadRequestException(
                "Role không được để trống.");
        }


        var newRole =
            request.Role
                .Trim()
                .ToLowerInvariant();


        if (!AllowedRoles.Contains(newRole))
        {
            throw new BadRequestException(
                "Role phải là customer, seller, shipper hoặc admin.");
        }


        // -------------------------------------------------
        // Get User
        // -------------------------------------------------

        var user =
            await _unitOfWork.Users
                .GetByIdAsync(userId);


        if (user == null)
        {
            throw new NotFoundException(
                $"User {userId} không tồn tại.");
        }


        // -------------------------------------------------
        // Get Role
        // -------------------------------------------------

        var roles =
            await _unitOfWork.Roles
                .FindAsync(
                    x =>
                        x.Name != null
                        &&
                        x.Name.ToLower() == newRole);


        var role =
            roles.FirstOrDefault();


        if (role == null)
        {
            throw new NotFoundException(
                $"Role '{newRole}' không tồn tại trong database.");
        }


        // -------------------------------------------------
        // Get current UserRoles
        // -------------------------------------------------

        var currentUserRoles =
            await _unitOfWork.UserRoles
                .FindAsync(
                    x =>
                        x.UserId == userId);


        // -------------------------------------------------
        // Nếu User đã có đúng Role duy nhất
        // -------------------------------------------------

        if (currentUserRoles.Count() == 1
            &&
            currentUserRoles.First().RoleId
                == role.Id)
        {
            user.UpdatedAt =
                DateTime.UtcNow;


            _unitOfWork.Users
                .Update(user);


            await _unitOfWork
                .SaveChangesAsync();


            return await MapToResponseAsync(user);
        }


        // -------------------------------------------------
        // Xóa Role cũ
        // -------------------------------------------------

        foreach (var userRole in currentUserRoles)
        {
            _unitOfWork.UserRoles
                .Delete(userRole);
        }


        // -------------------------------------------------
        // Tạo Role mới
        // -------------------------------------------------

        var newUserRole =
            new UserRoleModel
            {
                UserId = user.Id,

                RoleId = role.Id,

                AssignedAt = DateTime.UtcNow
            };


        await _unitOfWork.UserRoles
            .AddAsync(newUserRole);


        // -------------------------------------------------
        // Update User
        // -------------------------------------------------

        user.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Users
            .Update(user);


        // -------------------------------------------------
        // SAVE
        // -------------------------------------------------

        await _unitOfWork
            .SaveChangesAsync();


        return await MapToResponseAsync(user);
    }


    // =====================================================
    // GET ROLE NAMES
    // =====================================================

    private async Task<List<string>> GetRoleNamesAsync(
        long userId)
    {
        var userRoles =
            await _unitOfWork.UserRoles
                .FindAsync(
                    x =>
                        x.UserId == userId);


        var roleNames =
            new List<string>();


        foreach (var userRole in userRoles)
        {
            var role =
                await _unitOfWork.Roles
                    .GetByIdAsync(
                        userRole.RoleId);


            if (role == null)
            {
                continue;
            }


            if (!string.IsNullOrWhiteSpace(
                role.Name))
            {
                roleNames.Add(
                    role.Name);
            }
        }


        return roleNames;
    }


    // =====================================================
    // MAP USER -> RESPONSE DTO
    // =====================================================

    private async Task<UserResponseDTO>
        MapToResponseAsync(
            UserModel user)
    {
        var roles =
            await GetRoleNamesAsync(user.Id);


        return new UserResponseDTO
        {
            Id = user.Id,

            Email = user.Email,

            Phone = user.Phone,

            FullName = user.FullName,

            AvatarUrl = user.AvatarUrl,

            Status = user.Status,

            CreatedAt = user.CreatedAt,

            UpdatedAt = user.UpdatedAt,

            Roles = roles
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }
}