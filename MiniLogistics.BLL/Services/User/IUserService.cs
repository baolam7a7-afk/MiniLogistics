using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.User;

namespace MiniLogistics.BLL.Services.User;

public interface IUserService
{
    Task<PagedResponseDTO<UserResponseDTO>> GetAllAsync(
        UserPaginationRequestDTO request);

    Task<UserResponseDTO?> GetByIdAsync(
        long userId);

    Task<UserResponseDTO> LockAsync(
        long userId);

    Task<UserResponseDTO> UnlockAsync(
        long userId);

    Task<UserResponseDTO> UpdateRoleAsync(
        long userId,
        UpdateUserRoleDTO request);
}