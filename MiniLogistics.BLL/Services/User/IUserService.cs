using MiniLogistics.BLL.DTOs.User;

namespace MiniLogistics.BLL.Services.User;

public interface IUserService
{
    // GET ALL USERS
    Task<IEnumerable<UserResponseDTO>> GetAllAsync();

    // GET USER BY ID
    Task<UserResponseDTO?> GetByIdAsync(long userId);

    // LOCK USER
    Task<UserResponseDTO> LockAsync(long userId);

    // UNLOCK USER
    Task<UserResponseDTO> UnlockAsync(long userId);

    // UPDATE ROLE
    Task<UserResponseDTO> UpdateRoleAsync(
        long userId,
        UpdateUserRoleDTO request);
}