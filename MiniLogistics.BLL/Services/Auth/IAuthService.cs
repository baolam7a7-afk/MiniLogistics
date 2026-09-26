using MiniLogistics.BLL.DTOs.Auth;

namespace MiniLogistics.BLL.Services.Auth;

public interface IAuthService
{
    // =====================================================
    // REGISTER
    // =====================================================

    Task<AuthResponseDTO> RegisterAsync(
        RegisterRequestDTO request);


    // =====================================================
    // LOGIN
    // =====================================================

    Task<AuthResponseDTO> LoginAsync(
        LoginRequestDTO request);


    // =====================================================
    // GOOGLE LOGIN
    // =====================================================

    Task<AuthResponseDTO> GoogleLoginAsync(
        GoogleLoginRequestDTO request);


    // =====================================================
    // REFRESH TOKEN
    // =====================================================

    Task<AuthResponseDTO> RefreshTokenAsync(
        RefreshTokenRequestDTO request);


    // =====================================================
    // LOGOUT
    // =====================================================

    Task LogoutAsync(
        LogoutRequestDTO request);


    // =====================================================
    // FORGOT PASSWORD
    // =====================================================

    Task<string> ForgotPasswordAsync(
        ForgotPasswordRequestDTO request);


    // =====================================================
    // RESET PASSWORD
    // =====================================================

    Task ResetPasswordAsync(
        ResetPasswordRequestDTO request);
    // =====================================================
    // CHANGE PASSWORD
    // =====================================================

    Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequestDTO request);
}