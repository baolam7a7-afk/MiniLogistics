using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using MiniLogistics.API.Middleware;
using MiniLogistics.BLL.DTOs.Auth;
using MiniLogistics.BLL.Services.Auth;
using MiniLogistics.DAL.Data;

var builder = WebApplication.CreateBuilder(args);


// ==========================================================
// 1. DATABASE - SQL SERVER
// ==========================================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    );

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection chưa được cấu hình."
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});


// ==========================================================
// 2. JWT SETTINGS
// ==========================================================

var jwtSection =
    builder.Configuration.GetSection("Jwt");

builder.Services.Configure<JwtSettings>(
    jwtSection
);

var jwtSettings =
    jwtSection.Get<JwtSettings>();

if (jwtSettings == null)
{
    throw new InvalidOperationException(
        "Không đọc được cấu hình Jwt."
    );
}

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key chưa được cấu hình."
    );
}

if (jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key phải có ít nhất 32 ký tự."
    );
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer chưa được cấu hình."
    );
}

if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException(
        "Jwt:Audience chưa được cấu hình."
    );
}


// ==========================================================
// 3. DEPENDENCY INJECTION - SERVICES
// ==========================================================

// AuthService:
// - Register
// - Login
// - BCrypt
// - JWT
// - Refresh Token
// - Logout

builder.Services.AddScoped<
    IAuthService,
    AuthService
>();


// ==========================================================
// 4. JWT AUTHENTICATION
// ==========================================================

var signingKey =
    new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(
            jwtSettings.Key
        )
    );

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Kiểm tra chữ ký JWT
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = signingKey,

                // Kiểm tra Issuer
                ValidateIssuer = true,

                ValidIssuer = jwtSettings.Issuer,

                // Kiểm tra Audience
                ValidateAudience = true,

                ValidAudience = jwtSettings.Audience,

                // Kiểm tra thời gian hết hạn
                ValidateLifetime = true,

                // Không cho phép sai lệch thời gian
                ClockSkew = TimeSpan.Zero
            };
    });


// ==========================================================
// 5. AUTHORIZATION - PHÂN QUYỀN
// ==========================================================

builder.Services.AddAuthorization();


// ==========================================================
// 6. CONTROLLERS
// ==========================================================

builder.Services.AddControllers();


// ==========================================================
// 7. CORS - CHO BLAZOR
// ==========================================================

// Blazor của bạn:
// http://localhost:5107

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins chưa được cấu hình."
    );
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "BlazorPolicy",
        policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    );
});


// ==========================================================
// 8. SWAGGER
// ==========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // JWT Bearer
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Nhập Access Token JWT"
        }
    );

    options.AddSecurityRequirement(
        document =>
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        "Bearer",
                        document
                    )
                ] = []
            }
    );
});


// ==========================================================
// 9. BUILD APPLICATION
// ==========================================================

var app = builder.Build();


// ==========================================================
// 10. REQUEST LOGGING MIDDLEWARE
// ==========================================================

// Ghi:
// - RequestId
// - HTTP Method
// - URL
// - Status Code
// - Thời gian xử lý

app.UseMiddleware<RequestLoggingMiddleware>();


// ==========================================================
// 11. GLOBAL EXCEPTION MIDDLEWARE
// ==========================================================

// Bắt:
// - 400
// - 401
// - 403
// - 404
// - 500

app.UseMiddleware<GlobalExceptionMiddleware>();


// ==========================================================
// 12. SWAGGER
// ==========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// ==========================================================
// 13. HTTPS
// ==========================================================

// Nếu hiện tại bạn chạy API bằng HTTP
// http://localhost:5136
//
// và chưa cấu hình HTTPS,
// có thể tạm comment dòng này:
//
// app.UseHttpsRedirection();


// app.UseHttpsRedirection();


// ==========================================================
// 14. CORS
// ==========================================================

// Cho phép:
// Blazor http://localhost:5107
// gọi API http://localhost:5136

app.UseCors("BlazorPolicy");


// ==========================================================
// 15. AUTHENTICATION
// ==========================================================

// Xác định:
// "User này là ai?"
// JWT có hợp lệ không?

app.UseAuthentication();


// ==========================================================
// 16. AUTHORIZATION
// ==========================================================

// Kiểm tra:
// User có quyền truy cập API này không?

app.UseAuthorization();


// ==========================================================
// 17. CONTROLLERS
// ==========================================================

app.MapControllers();


// ==========================================================
// 18. RUN
// ==========================================================

app.Run();