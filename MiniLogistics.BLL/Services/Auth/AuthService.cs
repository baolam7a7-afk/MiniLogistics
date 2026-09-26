using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;

using BCrypt.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using MiniLogistics.BLL.DTOs.Auth;
using MiniLogistics.DAL.Data;

using UserModel = MiniLogistics.DAL.Models.User;
using UserRoleModel = MiniLogistics.DAL.Models.UserRole;
using UserSessionModel = MiniLogistics.DAL.Models.UserSession;
using PasswordResetTokenModel = MiniLogistics.DAL.Models.PasswordResetToken;

namespace MiniLogistics.BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly GoogleSettings _googleSettings;

    public AuthService(
        AppDbContext context,
        IOptions<JwtSettings> jwtOptions,
        IOptions<GoogleSettings> googleOptions)
    {
        _context = context;
        _jwtSettings = jwtOptions.Value;
        _googleSettings = googleOptions.Value;
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

        // =================================================
        // VALIDATE ROLE
        // =================================================

        string roleName =
            string.IsNullOrWhiteSpace(request.Role)
                ? "customer"
                : request.Role
                    .Trim()
                    .ToLowerInvariant();

        // Chỉ cho phép Customer / Seller / Shipper
        // tự đăng ký.
        //
        // Admin KHÔNG được tự đăng ký.
        var allowedRegisterRoles = new[]
        {
            "customer",
            "seller",
            "shipper"
        };

        if (!allowedRegisterRoles.Contains(roleName))
        {
            throw new Exception(
                "Bạn chỉ có thể đăng ký với role: customer, seller hoặc shipper.");
        }

        // =================================================
        // VALIDATE EMAIL
        // =================================================

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new Exception(
                "Email không được để trống.");
        }

        // =================================================
        // VALIDATE PASSWORD
        // =================================================

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new Exception(
                "Mật khẩu không được để trống.");
        }

        // =================================================
        // VALIDATE FULL NAME
        // =================================================

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

        // =================================================
        // PASSWORD LENGTH
        // =================================================

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
        // KIỂM TRA PHONE
        // =================================================

        string? phone = null;

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = request.Phone.Trim();

            bool phoneExists =
                await _context.Users
                    .AnyAsync(
                        x => x.Phone == phone);

            if (phoneExists)
            {
                throw new Exception(
                    "Số điện thoại đã được đăng ký.");
            }
        }

        // =================================================
        // TÌM ROLE
        // =================================================

        var role =
            await _context.Roles
                .FirstOrDefaultAsync(
                    x => x.Name == roleName);

        if (role == null)
        {
            throw new Exception(
                $"Role '{roleName}' chưa tồn tại trong Database.");
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

        PasswordHash = passwordHash,

        Phone = phone,

        FullName = fullName,

        Status = "active",

        AuthProvider = "local",

        GoogleId = null,

        CreatedAt = DateTime.UtcNow
    };

        _context.Users.Add(user);

        // Lưu User trước để Database
        // sinh User.Id
        await _context.SaveChangesAsync();

        // =================================================
        // GÁN ROLE
        // =================================================

        var userRole =
            new UserRoleModel
            {
                UserId =
                    user.Id,

                RoleId =
                    role.Id,

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
    // 3. GOOGLE LOGIN / REGISTER
    // =====================================================

    public async Task<AuthResponseDTO> GoogleLoginAsync(
        GoogleLoginRequestDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new Exception(
                "Google ID Token không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(_googleSettings.ClientId))
        {
            throw new Exception(
                "Google:ClientId chưa được cấu hình.");
        }

        // Google Login public chỉ tạo Customer.
        // Không cho client tự truyền role để tránh tự tạo Admin/Seller/Shipper.
        const string roleName = "customer";

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload =
                await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[]
                        {
                            _googleSettings.ClientId
                        }
                    });
        }
        catch
        {
            throw new Exception(
                "Google ID Token không hợp lệ.");
        }

        if (payload == null ||
            string.IsNullOrWhiteSpace(payload.Subject) ||
            string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new Exception(
                "Google account không có thông tin hợp lệ.");
        }

        string googleId = payload.Subject;

        string email =
            payload.Email
                .Trim()
                .ToLowerInvariant();

        string fullName =
            string.IsNullOrWhiteSpace(payload.Name)
                ? email
                : payload.Name.Trim();

        string? avatarUrl =
            string.IsNullOrWhiteSpace(payload.Picture)
                ? null
                : payload.Picture;

        // ==========================================
        // TÌM USER THEO GOOGLE ID
        // ==========================================

        var user =
            await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(
                    x => x.GoogleId == googleId);

        // ==========================================
        // CHƯA CÓ GOOGLE USER
        // ==========================================

        if (user == null)
        {
            // Không tự động link vào tài khoản local
            // chỉ vì email giống nhau.
            bool emailExists =
                await _context.Users
                    .AnyAsync(x => x.Email == email);

            if (emailExists)
            {
                throw new Exception(
                    "Email Google này đã tồn tại. Hãy đăng nhập bằng Email/Password hoặc thực hiện chức năng liên kết tài khoản.");
            }

            var role =
                await _context.Roles
                    .FirstOrDefaultAsync(
                        x => x.Name == roleName);

            if (role == null)
            {
                throw new Exception(
                    $"Role '{roleName}' chưa tồn tại trong Database.");
            }

            user =
                new UserModel
                {
                    Email = email,

                    // Google account không dùng password local.
                    PasswordHash = string.Empty,

                    Phone = null,

                    FullName = fullName,

                    AvatarUrl = avatarUrl,

                    Status = "active",

                    AuthProvider = "google",

                    GoogleId = googleId,

                    CreatedAt = DateTime.UtcNow
                };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            var userRole =
                new UserRoleModel
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow
                };

            _context.UserRoles.Add(userRole);

            await _context.SaveChangesAsync();

            user =
                await _context.Users
                    .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                    .FirstOrDefaultAsync(
                        x => x.Id == user.Id);

            if (user == null)
            {
                throw new Exception(
                    "Không thể tải lại Google User.");
            }
        }
        else
        {
            // ==========================================
            // GOOGLE USER ĐÃ TỒN TẠI
            // ==========================================

            if (user.Status != "active")
            {
                throw new Exception(
                    "Tài khoản hiện không hoạt động.");
            }

            bool profileChanged = false;

            if (!string.IsNullOrWhiteSpace(payload.Name) &&
                user.FullName != payload.Name)
            {
                user.FullName = payload.Name;
                profileChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(payload.Picture) &&
                user.AvatarUrl != payload.Picture)
            {
                user.AvatarUrl = payload.Picture;
                profileChanged = true;
            }

            if (profileChanged)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // Dùng chung JWT + Refresh Token hiện tại.
        return await CreateAuthResponseAsync(user);
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
    // =====================================================
    // 8. FORGOT PASSWORD
    // =====================================================

    public async Task<string> ForgotPasswordAsync(
        ForgotPasswordRequestDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new Exception(
                "Email không được để trống.");
        }

        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        // =================================================
        // TÌM USER
        // =================================================

        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Email == email);

        if (user == null)
        {
            throw new Exception(
                "Email không tồn tại.");
        }

        // =================================================
        // CHECK USER STATUS
        // =================================================

        if (user.Status != "active")
        {
            throw new Exception(
                "Tài khoản hiện không hoạt động.");
        }

        // =================================================
        // GOOGLE ACCOUNT
        // =================================================

        if (string.Equals(
                user.AuthProvider,
                "google",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Tài khoản này đăng nhập bằng Google. Vui lòng sử dụng Google Login.");
        }

        // =================================================
        // XÓA TOKEN RESET CŨ CHƯA DÙNG
        // =================================================

        var oldTokens =
            await _context.PasswordResetTokens
                .Where(
                    x =>
                        x.UserId == user.Id &&
                        x.UsedAt == null)
                .ToListAsync();

        if (oldTokens.Count > 0)
        {
            _context.PasswordResetTokens
                .RemoveRange(oldTokens);
        }

        // =================================================
        // TẠO RAW TOKEN
        // =================================================

        byte[] randomBytes =
            RandomNumberGenerator.GetBytes(64);

        string rawToken =
            Convert.ToBase64String(randomBytes);

        // =================================================
        // HASH TOKEN
        // =================================================

        string tokenHash =
            BCrypt.Net.BCrypt.HashPassword(
                rawToken);

        // =================================================
        // TẠO PASSWORD RESET TOKEN
        // =================================================

        var resetToken =
            new PasswordResetTokenModel
            {
                UserId = user.Id,

                TokenHash = tokenHash,

                CreatedAt = DateTime.UtcNow,

                // Token có hiệu lực 15 phút
                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(15),

                UsedAt = null
            };

        _context.PasswordResetTokens.Add(
            resetToken);

        await _context.SaveChangesAsync();

        // =================================================
        // DEVELOPMENT ONLY
        // =================================================
        // Hiện tại chưa cấu hình Email Service.
        // Trả raw token để test bằng Swagger.
        //
        // Sau này sẽ:
        // rawToken -> gửi vào Email
        // =================================================

        return rawToken;
    }
    // =====================================================
    // 9. RESET PASSWORD
    // =====================================================

    public async Task ResetPasswordAsync(
        ResetPasswordRequestDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        // =================================================
        // VALIDATE TOKEN
        // =================================================

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new Exception(
                "Reset Token không được để trống.");
        }

        // =================================================
        // VALIDATE NEW PASSWORD
        // =================================================

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new Exception(
                "Mật khẩu mới không được để trống.");
        }

        if (request.NewPassword.Length < 8)
        {
            throw new Exception(
                "Mật khẩu mới phải có ít nhất 8 ký tự.");
        }

        // =================================================
        // LOAD ACTIVE RESET TOKENS
        // =================================================

        var resetTokens =
            await _context.PasswordResetTokens
                .Include(x => x.User)
                .Where(
                    x =>
                        x.UsedAt == null &&
                        x.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

        // =================================================
        // FIND MATCHING TOKEN
        // =================================================

        PasswordResetTokenModel? matchedToken =
            null;

        foreach (var resetToken in resetTokens)
        {
            bool valid =
                BCrypt.Net.BCrypt.Verify(
                    request.Token,
                    resetToken.TokenHash);

            if (valid)
            {
                matchedToken = resetToken;
                break;
            }
        }

        // =================================================
        // TOKEN INVALID
        // =================================================

        if (matchedToken == null)
        {
            throw new Exception(
                "Reset Token không hợp lệ hoặc đã hết hạn.");
        }

        // =================================================
        // USER NOT FOUND
        // =================================================

        if (matchedToken.User == null)
        {
            throw new Exception(
                "Không tìm thấy User của Reset Token.");
        }

        // =================================================
        // CHECK USER STATUS
        // =================================================

        if (matchedToken.User.Status != "active")
        {
            throw new Exception(
                "Tài khoản hiện không hoạt động.");
        }

        // =================================================
        // GOOGLE ACCOUNT
        // =================================================

        if (string.Equals(
                matchedToken.User.AuthProvider,
                "google",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Tài khoản Google không sử dụng mật khẩu local.");
        }

        // =================================================
        // HASH PASSWORD MỚI
        // =================================================

        string newPasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        // =================================================
        // UPDATE USER PASSWORD
        // =================================================

        matchedToken.User.PasswordHash =
            newPasswordHash;

        matchedToken.User.UpdatedAt =
            DateTime.UtcNow;

        // =================================================
        // MARK TOKEN AS USED
        // =================================================

        matchedToken.UsedAt =
            DateTime.UtcNow;

        // =================================================
        // REVOKE ALL ACTIVE SESSIONS
        // =================================================
        // Khi đổi password thành công,
        // đăng xuất tất cả thiết bị đang đăng nhập.

        var activeSessions =
            await _context.UserSessions
                .Where(
                    x =>
                        x.UserId == matchedToken.UserId &&
                        x.RevokedAt == null)
                .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.RevokedAt =
                DateTime.UtcNow;
        }

        // =================================================
        // SAVE
        // =================================================

        await _context.SaveChangesAsync();
    }
    // =====================================================
    // 10. CHANGE PASSWORD
    // =====================================================

    public async Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequestDTO request)
    {
        // =================================================
        // VALIDATE REQUEST
        // =================================================

        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        // =================================================
        // VALIDATE CURRENT PASSWORD
        // =================================================

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new Exception(
                "Mật khẩu hiện tại không được để trống.");
        }

        // =================================================
        // VALIDATE NEW PASSWORD
        // =================================================

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new Exception(
                "Mật khẩu mới không được để trống.");
        }

        if (request.NewPassword.Length < 8)
        {
            throw new Exception(
                "Mật khẩu mới phải có ít nhất 8 ký tự.");
        }

        // =================================================
        // VALIDATE CONFIRM PASSWORD
        // =================================================

        if (string.IsNullOrWhiteSpace(
            request.ConfirmNewPassword))
        {
            throw new Exception(
                "Xác nhận mật khẩu mới không được để trống.");
        }

        if (request.NewPassword !=
            request.ConfirmNewPassword)
        {
            throw new Exception(
                "Mật khẩu mới và xác nhận mật khẩu không khớp.");
        }

        // =================================================
        // NEW PASSWORD MUST DIFFER
        // =================================================

        if (request.CurrentPassword ==
            request.NewPassword)
        {
            throw new Exception(
                "Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        // =================================================
        // FIND USER
        // =================================================

        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Id == userId);

        if (user == null)
        {
            throw new Exception(
                "Không tìm thấy tài khoản.");
        }

        // =================================================
        // CHECK USER STATUS
        // =================================================

        if (user.Status != "active")
        {
            throw new Exception(
                "Tài khoản hiện không hoạt động.");
        }

        // =================================================
        // GOOGLE ACCOUNT
        // =================================================

        if (string.Equals(
                user.AuthProvider,
                "google",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                "Tài khoản Google không sử dụng mật khẩu local.");
        }

        // =================================================
        // VERIFY CURRENT PASSWORD
        // =================================================

        bool currentPasswordValid =
            BCrypt.Net.BCrypt.Verify(
                request.CurrentPassword,
                user.PasswordHash);

        if (!currentPasswordValid)
        {
            throw new Exception(
                "Mật khẩu hiện tại không đúng.");
        }

        // =================================================
        // HASH NEW PASSWORD
        // =================================================

        string newPasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        // =================================================
        // UPDATE PASSWORD
        // =================================================

        user.PasswordHash =
            newPasswordHash;

        user.UpdatedAt =
            DateTime.UtcNow;

        // =================================================
        // REVOKE ALL ACTIVE SESSIONS
        // =================================================
        // Sau khi đổi mật khẩu,
        // đăng xuất các thiết bị đang đăng nhập.

        var activeSessions =
            await _context.UserSessions
                .Where(
                    x =>
                        x.UserId == userId &&
                        x.RevokedAt == null)
                .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.RevokedAt =
                DateTime.UtcNow;
        }

        // =================================================
        // SAVE
        // =================================================

        await _context.SaveChangesAsync();
    }
}