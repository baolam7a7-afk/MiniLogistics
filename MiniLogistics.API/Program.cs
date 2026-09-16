using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using MiniLogistics.BLL.DTOs.Auth;
using MiniLogistics.BLL.Services.Auth;
using MiniLogistics.DAL.Data;

var builder = WebApplication.CreateBuilder(args);


// ==========================================
// 1. DATABASE - SQL SERVER
// ==========================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection chưa được cấu hình."
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);


// ==========================================
// 2. JWT SETTINGS
// ==========================================

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


// ==========================================
// 3. DEPENDENCY INJECTION - SERVICES
// ==========================================

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


// ==========================================
// 4. JWT AUTHENTICATION
// ==========================================

var signingKey =
    new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtSettings.Key)
    );

builder.Services.AddAuthentication(options =>
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
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = signingKey,

            ValidateIssuer = true,

            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,

            ValidAudience = jwtSettings.Audience,

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };
});


// ==========================================
// 5. AUTHORIZATION - PHÂN QUYỀN
// ==========================================

builder.Services.AddAuthorization();


// ==========================================
// 6. CONTROLLERS
// ==========================================

builder.Services.AddControllers();


// ==========================================
// 7. CORS - CHO BLAZOR
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ==========================================
// 8. SWAGGER
// ==========================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
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

    options.AddSecurityRequirement(document =>
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


// ==========================================
// 9. BUILD APP
// ==========================================

var app = builder.Build();


// ==========================================
// 10. HTTP REQUEST PIPELINE
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("BlazorPolicy");

// Authentication phải đứng trước Authorization
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();