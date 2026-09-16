using MiniLogistics.BLL.DTOs.Auth;

namespace MiniLogistics.BLL.Services.Auth;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(
        RegisterRequestDTO request);

    Task<AuthResponseDTO> LoginAsync(
        LoginRequestDTO request);

    Task<AuthResponseDTO> RefreshTokenAsync(
        RefreshTokenRequestDTO request);

    Task LogoutAsync(
        LogoutRequestDTO request);
}