using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using MiniLogistics.API.Middleware;

using MiniLogistics.BLL.DTOs.Auth;
using MiniLogistics.BLL.Services;
using MiniLogistics.BLL.Services.Auth;
using MiniLogistics.BLL.Services.Inventory;
using MiniLogistics.BLL.Services.Cart;
using MiniLogistics.BLL.Services.Product;
using MiniLogistics.BLL.Services.Order;
using MiniLogistics.BLL.Services.Shipment;

using MiniLogistics.DAL.Data;
using MiniLogistics.DAL.Repositories;
using MiniLogistics.DAL.UnitOfWork;


// ==========================================================
// CREATE BUILDER
// ==========================================================

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
// 3. DEPENDENCY INJECTION - AUTH SERVICE
// ==========================================================

builder.Services.AddScoped<
    IAuthService,
    AuthService
>();


// ==========================================================
// 4. DEPENDENCY INJECTION - GENERIC REPOSITORY
// ==========================================================

builder.Services.AddScoped(
    typeof(IRepository<>),
    typeof(Repository<>)
);


// ==========================================================
// 5. DEPENDENCY INJECTION - UNIT OF WORK
// ==========================================================

builder.Services.AddScoped<
    IUnitOfWork,
    UnitOfWork
>();


// ==========================================================
// 6. DEPENDENCY INJECTION - PRODUCT SERVICE
// ==========================================================

builder.Services.AddScoped<
    IProductService,
    ProductService
>();


// ==========================================================
// 7. DEPENDENCY INJECTION - PRODUCT VARIANT SERVICE
// ==========================================================

// ProductVariantController
//        ↓
// IProductVariantService
//        ↓
// ProductVariantService
//        ↓
// IUnitOfWork
//        ↓
// Repository<ProductVariant>
//        +
// Repository<Inventory>

builder.Services.AddScoped<
    IProductVariantService,
    ProductVariantService
>();


// ==========================================================
// 8. DEPENDENCY INJECTION - CATEGORY SERVICE
// ==========================================================

// CategoryController
//        ↓
// ICategoryService
//        ↓
// CategoryService
//        ↓
// IUnitOfWork
//        ↓
// Repository<Category>

builder.Services.AddScoped<
    ICategoryService,
    CategoryService
>();


// ==========================================================
// 9. DEPENDENCY INJECTION - INVENTORY SERVICE
// ==========================================================

// InventoryController
//        ↓
// IInventoryService
//        ↓
// InventoryService
//        ↓
// IUnitOfWork
//        ↓
// Repository<Inventory>
//        +
// Repository<ProductVariant>

builder.Services.AddScoped<
    IInventoryService,
    InventoryService
>();


// ==========================================================
// 10. DEPENDENCY INJECTION - CART SERVICE
// ==========================================================

// CartController
//        ↓
// ICartService
//        ↓
// CartService
//        ↓
// IUnitOfWork
//        ↓
// Repository<Cart>
//        +
// Repository<CartItem>
//        +
// Repository<ProductVariant>
//        +
// Repository<Inventory>

builder.Services.AddScoped<
    ICartService,
    CartService
>();


// ==========================================================
// 11. DEPENDENCY INJECTION - ORDER SERVICE
// ==========================================================

builder.Services.AddScoped<
    IOrderService,
    OrderService
>();


// ==========================================================
// 12. DEPENDENCY INJECTION - SHIPMENT SERVICE
// ==========================================================

builder.Services.AddScoped<
    IShipmentService,
    ShipmentService
>();


// ==========================================================
// 13. JWT AUTHENTICATION
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
                // ------------------------------------------
                // Kiểm tra chữ ký JWT
                // ------------------------------------------

                ValidateIssuerSigningKey = true,

                IssuerSigningKey = signingKey,


                // ------------------------------------------
                // Kiểm tra Issuer
                // ------------------------------------------

                ValidateIssuer = true,

                ValidIssuer =
                    jwtSettings.Issuer,


                // ------------------------------------------
                // Kiểm tra Audience
                // ------------------------------------------

                ValidateAudience = true,

                ValidAudience =
                    jwtSettings.Audience,


                // ------------------------------------------
                // Kiểm tra thời gian hết hạn
                // ------------------------------------------

                ValidateLifetime = true,

                // Không cho phép sai lệch thời gian
                ClockSkew =
                    TimeSpan.Zero
            };
    });


// ==========================================================
// 14. AUTHORIZATION
// ==========================================================

builder.Services.AddAuthorization();


// ==========================================================
// 15. CONTROLLERS
// ==========================================================

builder.Services.AddControllers();


// ==========================================================
// 16. CORS - CHO BLAZOR
// ==========================================================

// Blazor:
// http://localhost:5107
//
// API:
// http://localhost:5136

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
// 17. SWAGGER
// ==========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // ------------------------------------------
    // JWT Bearer
    // ------------------------------------------

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


    // ------------------------------------------
    // Swagger Security Requirement
    // ------------------------------------------

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
// 18. BUILD APPLICATION
// ==========================================================

var app = builder.Build();


// ==========================================================
// 19. REQUEST LOGGING MIDDLEWARE
// ==========================================================

app.UseMiddleware<RequestLoggingMiddleware>();


// ==========================================================
// 20. GLOBAL EXCEPTION MIDDLEWARE
// ==========================================================

app.UseMiddleware<GlobalExceptionMiddleware>();


// ==========================================================
// 21. SWAGGER
// ==========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// ==========================================================
// 22. HTTPS
// ==========================================================

// Hiện tại project chạy HTTP:
//
// API:
// http://localhost:5136
//
// Blazor:
// http://localhost:5107
//
// Tạm thời không bật:
//
// app.UseHttpsRedirection();


// ==========================================================
// 23. CORS
// ==========================================================

app.UseCors("BlazorPolicy");


// ==========================================================
// 24. AUTHENTICATION
// ==========================================================

app.UseAuthentication();


// ==========================================================
// 25. AUTHORIZATION
// ==========================================================

app.UseAuthorization();


// ==========================================================
// 26. MAP CONTROLLERS
// ==========================================================

app.MapControllers();


// ==========================================================
// 27. RUN
// ==========================================================

app.Run();