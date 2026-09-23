using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using MiniLogistics.API.Middleware;

using MiniLogistics.BLL.DTOs.Auth;

using MiniLogistics.BLL.Services;
using MiniLogistics.BLL.Services.Auth;
using MiniLogistics.BLL.Services.Cart;
using MiniLogistics.BLL.Services.Inventory;
using MiniLogistics.BLL.Services.Order;
using MiniLogistics.BLL.Services.Payment;
using MiniLogistics.BLL.Services.Product;
using MiniLogistics.BLL.Services.ProductImage;
using MiniLogistics.BLL.Services.Review;
using MiniLogistics.BLL.Services.ReviewReply;
using MiniLogistics.BLL.Services.Shipment;
using MiniLogistics.BLL.Services.SupportTicket;
using MiniLogistics.BLL.Services.SupportMessage;
using MiniLogistics.BLL.Services.Dispute;
using MiniLogistics.BLL.Services.DisputeMessage;
using MiniLogistics.BLL.Services.ReportSnapshot;
using MiniLogistics.BLL.Services.Voucher;

using MiniLogistics.DAL.Data;
using MiniLogistics.DAL.Repositories;
using MiniLogistics.DAL.UnitOfWork;


// =====================================================
// 1. CREATE BUILDER
// =====================================================

var builder =
    WebApplication.CreateBuilder(args);


// =====================================================
// 2. DATABASE
// =====================================================

var connectionString =
    builder.Configuration
        .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection chưa được cấu hình.");
}

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseSqlServer(connectionString);
    });


// =====================================================
// 3. JWT SETTINGS
// =====================================================

var jwtSection =
    builder.Configuration.GetSection("Jwt");

builder.Services.Configure<JwtSettings>(
    jwtSection);

var jwtSettings =
    jwtSection.Get<JwtSettings>();

if (jwtSettings == null)
{
    throw new InvalidOperationException(
        "Không đọc được cấu hình Jwt.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key chưa được cấu hình.");
}

if (jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key phải có ít nhất 32 ký tự.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer chưa được cấu hình.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException(
        "Jwt:Audience chưa được cấu hình.");
}


// =====================================================
// 4. AUTH SERVICE
// =====================================================

builder.Services.AddScoped<
    IAuthService,
    AuthService>();


// =====================================================
// 5. GENERIC REPOSITORY
// =====================================================

builder.Services.AddScoped(
    typeof(IRepository<>),
    typeof(Repository<>));


// =====================================================
// 6. UNIT OF WORK
// =====================================================

builder.Services.AddScoped<
    IUnitOfWork,
    UnitOfWork>();


// =====================================================
// 7. PRODUCT
// =====================================================

builder.Services.AddScoped<
    IProductService,
    ProductService>();


// =====================================================
// 8. PRODUCT VARIANT
// =====================================================

builder.Services.AddScoped<
    IProductVariantService,
    ProductVariantService>();


// =====================================================
// 9. PRODUCT IMAGE
// =====================================================

builder.Services.AddScoped<
    IProductImageService,
    ProductImageService>();


// =====================================================
// 10. CATEGORY
// =====================================================

builder.Services.AddScoped<
    ICategoryService,
    CategoryService>();


// =====================================================
// 11. INVENTORY
// =====================================================

builder.Services.AddScoped<
    IInventoryService,
    InventoryService>();


// =====================================================
// 12. CART
// =====================================================

builder.Services.AddScoped<
    ICartService,
    CartService>();


// =====================================================
// 13. ORDER
// =====================================================

builder.Services.AddScoped<
    IOrderService,
    OrderService>();


// =====================================================
// 14. SHIPMENT
// =====================================================

builder.Services.AddScoped<
    IShipmentService,
    ShipmentService>();


// =====================================================
// 15. PAYMENT
// =====================================================

builder.Services.AddScoped<
    IPaymentService,
    PaymentService>();


// =====================================================
// 16. VOUCHER
// =====================================================

builder.Services.AddScoped<
    IVoucherService,
    VoucherService>();


// =====================================================
// 17. REVIEW
// =====================================================

builder.Services.AddScoped<
    IReviewService,
    ReviewService>();


// =====================================================
// 18. REVIEW REPLY
// =====================================================

builder.Services.AddScoped<
    IReviewReplyService,
    ReviewReplyService>();


// =====================================================
// 19. SUPPORT TICKET
// =====================================================

builder.Services.AddScoped<
    ISupportTicketService,
    SupportTicketService>();


builder.Services.AddScoped<
    ISupportMessageService,
    SupportMessageService>();

builder.Services.AddScoped<
    IDisputeService,
    DisputeService>();
builder.Services.AddScoped<
    IDisputeMessageService,
    DisputeMessageService>();
builder.Services.AddScoped<
    IReportSnapshotService,
    ReportSnapshotService>();
// =====================================================
// 20. JWT AUTHENTICATION
// =====================================================

var signingKey =
    new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(
            jwtSettings.Key));


builder.Services
    .AddAuthentication(
        options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,

                    IssuerSigningKey =
                        signingKey,

                    ValidateIssuer = true,

                    ValidIssuer =
                        jwtSettings.Issuer,

                    ValidateAudience = true,

                    ValidAudience =
                        jwtSettings.Audience,

                    ValidateLifetime = true,

                    ClockSkew =
                        TimeSpan.Zero
                };
        });


// =====================================================
// 21. AUTHORIZATION
// =====================================================

builder.Services.AddAuthorization();


// =====================================================
// 22. CONTROLLERS
// =====================================================

builder.Services.AddControllers();


// =====================================================
// 23. CORS
// =====================================================

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>();

if (allowedOrigins == null ||
    allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins chưa được cấu hình.");
}

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "BlazorPolicy",
            policy =>
            {
                policy
                    .WithOrigins(
                        allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });


// =====================================================
// 24. SWAGGER
// =====================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
    options =>
    {
        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",

                Type =
                    SecuritySchemeType.Http,

                Scheme = "bearer",

                BearerFormat = "JWT",

                In =
                    ParameterLocation.Header,

                Description =
                    "Nhập Access Token JWT"
            });

        options.AddSecurityRequirement(
            document =>
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference(
                            "Bearer",
                            document)
                    ] = []
                });
    });


// =====================================================
// 25. BUILD
// =====================================================

var app =
    builder.Build();


// =====================================================
// 26. REQUEST LOGGING
// =====================================================

app.UseMiddleware<
    RequestLoggingMiddleware>();


// =====================================================
// 27. GLOBAL EXCEPTION
// =====================================================

app.UseMiddleware<
    GlobalExceptionMiddleware>();


// =====================================================
// 28. SWAGGER
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// =====================================================
// 29. CORS
// =====================================================

app.UseCors("BlazorPolicy");


// =====================================================
// 30. AUTHENTICATION
// =====================================================

app.UseAuthentication();


// =====================================================
// 31. AUTHORIZATION
// =====================================================

app.UseAuthorization();


// =====================================================
// 32. CONTROLLERS
// =====================================================

app.MapControllers();


// =====================================================
// 33. RUN
// =====================================================

app.Run();