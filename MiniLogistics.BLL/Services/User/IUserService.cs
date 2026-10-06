using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.User;

namespace MiniLogistics.BLL.Services.User;

public interface IUserService
{
    // =====================================================
    // ADMIN - GET ALL USERS
    // =====================================================

    Task<PagedResponseDTO<UserResponseDTO>> GetAllAsync(
        UserPaginationRequestDTO request);

    // =====================================================
    // ADMIN - GET USER BY ID
    // =====================================================

    Task<UserResponseDTO?> GetByIdAsync(
        long userId);

    // =====================================================
    // ADMIN - LOCK USER
    // =====================================================

    Task<UserResponseDTO> LockAsync(
        long userId);

    // =====================================================
    // ADMIN - UNLOCK USER
    // =====================================================

    Task<UserResponseDTO> UnlockAsync(
        long userId);

    // =====================================================
    // ADMIN - UPDATE ROLE
    // =====================================================

    Task<UserResponseDTO> UpdateRoleAsync(
        long userId,
        UpdateUserRoleDTO request);

    // =====================================================
    // CUSTOMER / USER - GET MY PROFILE
    // =====================================================

    Task<UserResponseDTO?> GetMyProfileAsync(
        long userId);

    // =====================================================
    // CUSTOMER / USER - UPDATE MY PROFILE
    // =====================================================

    Task<UserResponseDTO> UpdateMyProfileAsync(
        long userId,
        UpdateMyProfileDTO request);

    Task<string?> ReplaceAvatarUrlAsync(
        long userId,
        string avatarUrl);
}