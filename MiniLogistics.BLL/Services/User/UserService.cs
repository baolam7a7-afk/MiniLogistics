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

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =====================================================
    // GET ALL USERS
    // =====================================================

    public async Task<IEnumerable<UserResponseDTO>> GetAllAsync()
    {
        var users =
            await _unitOfWork.Users
                .GetAllAsync();

        var result =
            new List<UserResponseDTO>();

        foreach (var user in users)
        {
            result.Add(
                await MapToResponseAsync(user));
        }

        return result
            .OrderBy(x => x.Id)
            .ToList();
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
            x => string.Equals(
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
                        x.Name != null &&
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
                    x => x.UserId == userId);

        // -------------------------------------------------
        // Nếu User đã có đúng Role duy nhất
        // -------------------------------------------------

        if (currentUserRoles.Count() == 1 &&
            currentUserRoles.First().RoleId == role.Id)
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
                    x => x.UserId == userId);

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

            if (!string.IsNullOrWhiteSpace(role.Name))
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

    private async Task<UserResponseDTO> MapToResponseAsync(
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