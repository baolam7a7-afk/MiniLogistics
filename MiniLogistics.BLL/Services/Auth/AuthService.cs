using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using BCrypt.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using MiniLogistics.BLL.DTOs.Auth;
using MiniLogistics.DAL.Data;

using UserModel = MiniLogistics.DAL.Models.User;
using UserRoleModel = MiniLogistics.DAL.Models.UserRole;
using UserSessionModel = MiniLogistics.DAL.Models.UserSession;

namespace MiniLogistics.BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        AppDbContext context,
        IOptions<JwtSettings> jwtOptions)
    {
        _context = context;
        _jwtSettings = jwtOptions.Value;
    }

    // =====================================================
    // 1. REGISTER
    // =====================================================

    public async Task<AuthResponseDTO> RegisterAsync(
        RegisterRequestDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new Exception(
                "Email không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new Exception(
                "Mật khẩu không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new Exception(
                "Họ tên không được để trống.");
        }

        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        string fullName =
            request.FullName.Trim();

        if (request.Password.Length < 8)
        {
            throw new Exception(
                "Mật khẩu phải có ít nhất 8 ký tự.");
        }

        // =================================================
        // KIỂM TRA EMAIL
        // =================================================

        bool emailExists =
            await _context.Users
                .AnyAsync(
                    x => x.Email == email);

        if (emailExists)
        {
            throw new Exception(
                "Email đã được đăng ký.");
        }

        // =================================================
        // TÌM ROLE CUSTOMER
        // =================================================

        var customerRole =
            await _context.Roles
                .FirstOrDefaultAsync(
                    x => x.Name == "customer");

        if (customerRole == null)
        {
            throw new Exception(
                "Role customer chưa tồn tại trong Database.");
        }

        // =================================================
        // HASH PASSWORD
        // =================================================

        string passwordHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.Password);

        // =================================================
        // TẠO USER
        // =================================================

        var user =
            new UserModel
            {
                Email = email,

                PasswordHash =
                    passwordHash,

                Phone =
                    string.IsNullOrWhiteSpace(
                        request.Phone)
                        ? null
                        : request.Phone.Trim(),

                FullName =
                    fullName,

                Status =
                    "active",

                CreatedAt =
                    DateTime.UtcNow
            };

        _context.Users.Add(user);

        // Lưu User trước
        // để Database sinh User.Id

        await _context.SaveChangesAsync();

        // =================================================
        // GÁN ROLE CUSTOMER
        // =================================================

        var userRole =
            new UserRoleModel
            {
                UserId =
                    user.Id,

                RoleId =
                    customerRole.Id,

                AssignedAt =
                    DateTime.UtcNow
            };

        _context.UserRoles.Add(userRole);

        await _context.SaveChangesAsync();

        // =================================================
        // LOAD LẠI USER + ROLE
        // =================================================

        var userWithRoles =
            await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(
                    x => x.Id == user.Id);

        if (userWithRoles == null)
        {
            throw new Exception(
                "Không thể tải lại User sau khi đăng ký.");
        }

        // =================================================
        // TẠO AUTH RESPONSE
        // =================================================

        return await CreateAuthResponseAsync(
            userWithRoles);
    }


    // =====================================================
    // 2. LOGIN
    // =====================================================

    public async Task<AuthResponseDTO> LoginAsync(
        LoginRequestDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new Exception(
                "Email không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new Exception(
                "Mật khẩu không được để trống.");
        }

        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        // =================================================
        // LOAD USER + ROLE
        // =================================================

        var user =
            await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(
                    x => x.Email == email);

        if (user == null)
        {
            throw new Exception(
                "Email hoặc mật khẩu không đúng.");
        }

        // =================================================
        // CHECK STATUS
        // =================================================

        if (user.Status != "active")
        {
            throw new Exception(
                "Tài khoản hiện không hoạt động.");
        }

        // =================================================
        // VERIFY PASSWORD
        // =================================================

        bool passwordValid =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

        if (!passwordValid)
        {
            throw new Exception(
                "Email hoặc mật khẩu không đúng.");
        }

        // =================================================
        // CREATE AUTH RESPONSE
        // =================================================

        return await CreateAuthResponseAsync(
            user);
    }


    // =====================================================
    // 3. CREATE AUTH RESPONSE
    // =====================================================

    private async Task<AuthResponseDTO>
        CreateAuthResponseAsync(
            UserModel user)
    {
        // =================================================
        // LẤY ROLE
        // =================================================

        var roles =
            user.UserRoles?
                .Where(
                    x => x.Role != null)
                .Select(
                    x => x.Role.Name)
                .ToList()
            ?? new List<string>();

        // =================================================
        // ACCESS TOKEN
        // =================================================

        string accessToken =
            GenerateAccessToken(
                user,
                roles);

        // =================================================
        // REFRESH TOKEN
        // =================================================

        string refreshToken =
            GenerateRefreshToken();

        // =================================================
        // HASH REFRESH TOKEN
        // =================================================

        string refreshTokenHash =
            BCrypt.Net.BCrypt.HashPassword(
                refreshToken);

        // =================================================
        // TOKEN EXPIRATION
        // =================================================

        DateTime accessTokenExpiresAt =
            DateTime.UtcNow.AddMinutes(
                _jwtSettings.AccessTokenMinutes);

        DateTime refreshTokenExpiresAt =
            DateTime.UtcNow.AddDays(
                _jwtSettings.RefreshTokenDays);

        // =================================================
        // CREATE USER SESSION
        // =================================================

        var session =
            new UserSessionModel
            {
                UserId =
                    user.Id,

                RefreshTokenHash =
                    refreshTokenHash,

                CreatedAt =
                    DateTime.UtcNow,

                ExpiresAt =
                    refreshTokenExpiresAt,

                RevokedAt =
                    null
            };

        _context.UserSessions.Add(
            session);

        await _context.SaveChangesAsync();

        // =================================================
        // RESPONSE
        // =================================================

        return new AuthResponseDTO
        {
            UserId =
                user.Id,

            Email =
                user.Email,

            FullName =
                user.FullName
                ?? string.Empty,

            Roles =
                roles,

            AccessToken =
                accessToken,

            RefreshToken =
                refreshToken,

            AccessTokenExpiresAt =
                accessTokenExpiresAt,

            RefreshTokenExpiresAt =
                refreshTokenExpiresAt
        };
    }


    // =====================================================
    // 4. GENERATE ACCESS TOKEN
    // =====================================================

    private string GenerateAccessToken(
        UserModel user,
        List<string> roles)
    {
        // =================================================
        // CLAIMS
        // =================================================

        var claims =
            new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName
                    ?? user.Email)
            };

        // =================================================
        // ROLE CLAIMS
        // =================================================

        foreach (string role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        // =================================================
        // CHECK JWT KEY
        // =================================================

        if (string.IsNullOrWhiteSpace(
            _jwtSettings.Key))
        {
            throw new Exception(
                "Jwt:Key không được để trống.");
        }

        // =================================================
        // SIGNING KEY
        // =================================================

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _jwtSettings.Key));

        // =================================================
        // SIGNING CREDENTIALS
        // =================================================

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        // =================================================
        // CREATE JWT
        // =================================================

        var token =
            new JwtSecurityToken(
                issuer:
                    _jwtSettings.Issuer,

                audience:
                    _jwtSettings.Audience,

                claims:
                    claims,

                expires:
                    DateTime.UtcNow.AddMinutes(
                        _jwtSettings.AccessTokenMinutes),

                signingCredentials:
                    credentials);

        // =================================================
        // RETURN TOKEN
        // =================================================

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }


    // =====================================================
    // 5. GENERATE REFRESH TOKEN
    // =====================================================

    private string GenerateRefreshToken()
    {
        byte[] randomBytes =
            RandomNumberGenerator.GetBytes(
                64);

        return Convert.ToBase64String(
            randomBytes);
    }


    // =====================================================
    // 6. REFRESH TOKEN
    // =====================================================

    public async Task<AuthResponseDTO>
        RefreshTokenAsync(
            RefreshTokenRequestDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(
            request.RefreshToken))
        {
            throw new Exception(
                "Refresh Token không được để trống.");
        }

        // =================================================
        // LOAD SESSION
        // =================================================

        var sessions =
            await _context.UserSessions
                .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Where(
                    x =>
                        x.RevokedAt == null &&
                        x.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

        UserSessionModel? matchedSession =
            null;

        // =================================================
        // FIND MATCHED SESSION
        // =================================================

        foreach (var session in sessions)
        {
            bool valid =
                BCrypt.Net.BCrypt.Verify(
                    request.RefreshToken,
                    session.RefreshTokenHash);

            if (valid)
            {
                matchedSession =
                    session;

                break;
            }
        }

        // =================================================
        // SESSION NOT FOUND
        // =================================================

        if (matchedSession == null)
        {
            throw new Exception(
                "Refresh Token không hợp lệ hoặc đã hết hạn.");
        }

        // =================================================
        // USER NOT FOUND
        // =================================================

        if (matchedSession.User == null)
        {
            throw new Exception(
                "Không tìm thấy User của Refresh Token.");
        }

        // =================================================
        // CHECK USER STATUS
        // =================================================

        if (matchedSession.User.Status != "active")
        {
            throw new Exception(
                "Tài khoản hiện không hoạt động.");
        }

        // =================================================
        // REVOKE OLD REFRESH TOKEN
        // =================================================

        matchedSession.RevokedAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // =================================================
        // CREATE NEW TOKEN
        // =================================================

        return await CreateAuthResponseAsync(
            matchedSession.User);
    }


    // =====================================================
    // 7. LOGOUT
    // =====================================================

    public async Task LogoutAsync(
        LogoutRequestDTO request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(
                request.RefreshToken))
        {
            return;
        }

        // =================================================
        // LOAD ACTIVE SESSIONS
        // =================================================

        var sessions =
            await _context.UserSessions
                .Where(
                    x =>
                        x.RevokedAt == null &&
                        x.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

        // =================================================
        // FIND REFRESH TOKEN
        // =================================================

        foreach (var session in sessions)
        {
            bool valid =
                BCrypt.Net.BCrypt.Verify(
                    request.RefreshToken,
                    session.RefreshTokenHash);

            if (valid)
            {
                session.RevokedAt =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return;
            }
        }
    }
}