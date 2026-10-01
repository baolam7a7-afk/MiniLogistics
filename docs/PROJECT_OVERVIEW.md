# MiniLogistics — Tổng quan dự án

Tài liệu mô tả cấu trúc, domain, và phạm vi hiện tại của solution (cập nhật sau khi đọc toàn bộ source).

---

## 1. Cấu trúc solution

```
MiniLogistics-master/
├── LogisticsShop.slnx
├── MiniLogistics.API/          # Host REST API
│   ├── Controllers/            # ~32 controllers
│   ├── Middleware/             # Logging + GlobalException
│   ├── wwwroot/uploads/        # Avatar / static files
│   ├── Program.cs
│   └── appsettings.json
├── MiniLogistics.BLL/          # Business logic
│   ├── DTOs/                   # Create/Update/Response + pagination
│   ├── Exceptions/             # BadRequest, NotFound, Forbidden, Unauthorized
│   └── Services/               # ~29 domain services
├── MiniLogistics.DAL/          # Data access
│   ├── Data/AppDbContext.cs
│   ├── Models/                 # 34 entities
│   ├── Repositories/
│   ├── UnitOfWork/
│   └── Migrations/             # 4 migrations
└── MiniLogistics.Web/          # Blazor WASM (customer UI)
    ├── Pages/Auth, Pages/Customer
    ├── Services/               # HTTP clients
    ├── Models/                 # Mirror API contracts
    └── Layout/
```

### Quan hệ project

| Project | Tham chiếu |
|---------|------------|
| API | → BLL, DAL |
| BLL | → DAL |
| DAL | (không phụ thuộc project khác) |
| Web | **không** reference BLL — gọi API qua `HttpClient` |

---

## 2. Tech stack chi tiết

| Hạng mục | Chi tiết |
|----------|----------|
| Runtime | .NET 10 (`net10.0`) |
| ORM | EF Core 10 + SQL Server |
| Auth | JWT Bearer (HS256) + refresh token (`UserSession`) |
| Social login | Google ID token (`Google.Apis.Auth`) — validate phía server |
| Password | BCrypt.Net-Next |
| API docs | Swashbuckle / OpenAPI (chỉ Development) |
| Frontend | Blazor WebAssembly 10 |
| Pattern | Layered + Generic Repository + Unit of Work |

---

## 3. Domain & database

### 3.1 Nhóm nghiệp vụ

| Nhóm | Entities chính |
|------|----------------|
| Identity | `User`, `Role`, `UserRole`, `UserSession`, `PasswordResetToken` |
| Catalog | `Category`, `Shop`, `Product`, `ProductImage`, `ProductVariant`, `Inventory` |
| Commerce | `Cart`, `CartItem`, `Order`, `OrderItem`, `OrderStatusLog`, `OrderVoucher`, `Voucher` |
| Payment | `PaymentTransaction`, `RefundTransaction` |
| Logistics | `Shipment`, `ShipmentEvent`, `ReturnRequest` |
| Engagement | `Review`, `ReviewReply` |
| Support | `SupportTicket`, `SupportMessage`, `Dispute`, `DisputeMessage` |
| Seller finance | `ShopWallet`, `ShopWalletTransaction`, `PayoutRequest` |
| Reporting | `ReportSnapshot` |
| Address | `Address` |

### 3.2 Quan hệ quan trọng

- User 1–N Shop (seller); Shop 1–1 ShopWallet.
- Product → Variants → Inventory (1–1).
- Cart 1–1 User; Order thuộc 1 Customer + 1 Shop + 1 Address.
- Order 1–1 Shipment; PaymentTransaction gắn Order.
- ReturnRequest → RefundTransaction; Dispute gắn Order.

### 3.3 Migrations

| Migration | Mục đích |
|-----------|----------|
| `InitialCreate` | Schema đầy đủ ban đầu |
| `AddInventory` | Tách `Stock` trên variant → bảng `inventories` |
| `AddGoogleAuthentication` | `AuthProvider`, `GoogleId` trên `users` |
| `AddPasswordResetToken` | Bảng reset mật khẩu |

DbContext: `MiniLogistics.DAL/Data/AppDbContext.cs` — cấu hình Fluent API tập trung (table name, index, precision, cascade).

---

## 4. API layer (`MiniLogistics.API`)

### 4.1 Pipeline (`Program.cs`)

1. Static files (`wwwroot`)
2. `RequestLoggingMiddleware`
3. `GlobalExceptionMiddleware`
4. Swagger (Development)
5. CORS (`BlazorPolicy`)
6. Authentication → Authorization
7. Controllers

### 4.2 Controllers theo vai trò

**Auth / User**

| Controller | Route | Chức năng |
|------------|-------|-----------|
| AuthController | `api/auth` | Register, login, Google, refresh, logout, forgot/reset/change password |
| UserController | `api/users` | Profile + admin lock/unlock/role |
| AvatarController | `api/users/me/avatar` | Upload avatar |
| AddressController | `api/addresses` | CRUD địa chỉ |

**Customer**

| Controller | Chức năng |
|------------|-----------|
| CartController | Giỏ hàng |
| OrderController | Tạo / xem / hủy đơn |
| OrderVoucherController | Áp voucher vào đơn pending |
| PaymentController | Xem thanh toán (admin cập nhật status) |
| ReviewController | Đánh giá |
| ReturnRequestController | Yêu cầu trả hàng |
| RefundTransactionsController | Hoàn tiền (admin tạo/complete) |

**Seller**

| Controller | Chức năng |
|------------|-----------|
| ShopController | Tạo/sửa shop; admin duyệt |
| Product / Variant / Image | Catalog seller |
| InventoryController | Tăng/giảm/điều chỉnh/reserve/release |
| VoucherController | CRUD + validate |
| SellerDashboardController | KPI seller |
| ShopWallet* / PayoutRequest | Ví & rút tiền |

**Shipper / Admin / Shared**

| Controller | Chức năng |
|------------|-----------|
| ShipmentController | Tạo, gán shipper, cập nhật status |
| AdminDashboardController | Tổng quan nền tảng |
| CategoryController | Danh mục (admin CUD) |
| Support* / Dispute* | Hỗ trợ & tranh chấp |
| ReportSnapshotController | Snapshot báo cáo |
| TestController / ExceptionTestController | Probe auth / exception (dev) |

### 4.3 Auth & cấu hình

- JWT claims: `NameIdentifier`, `Email`, `Name`, `Role`(s).
- Roles: `customer`, `seller`, `admin`, `shipper`.
- CORS origins: `Cors:AllowedOrigins` (hiện `http://localhost:5107`).
- Google: client gửi ID token → BLL validate theo `Google:ClientId`.

---

## 5. BLL layer (`MiniLogistics.BLL`)

### 5.1 Services (~29)

Auth, User, Address, Category, Shop, Product, ProductVariant, ProductImage, Cart, Order, Payment, OrderVoucher, Voucher, Inventory, Shipment, Review, ReviewReply, ReturnRequest, RefundTransaction, Dispute, DisputeMessage, SupportTicket, SupportMessage, ShopWallet, ShopWalletTransaction, PayoutRequest, SellerDashboard, AdminDashboard, ReportSnapshot.

### 5.2 Luồng nghiệp vụ chính

**Đăng ký / đăng nhập**

- Register: role whitelist `customer|seller|shipper`, BCrypt, gán role, phát JWT + refresh session.
- Login / Google login / refresh / logout / forgot-reset-change password.

**Checkout (COD)**

1. Cart kiểm tra stock = `Quantity - ReservedQuantity`.
2. `OrderService.CreateAsync`: chỉ nhận `paymentMethod = "cod"` (normalize lower-case).
3. Trong transaction: reserve inventory, tạo order + items + `PaymentTransaction` (pending) + status log.
4. Discount lúc tạo đơn = 0; voucher áp sau qua `OrderVoucherService` khi đơn còn `pending`.
5. Cancel (pending): release inventory + release voucher + cancel payment pending.

**Inventory**

- `InventoryService`: increase / decrease / adjust / reserve / release / deduct.
- `OrderService` đang **tự implement** reserve/release (trùng logic với InventoryService).

**Returns / refunds / wallet**

- Return request → approve/reject.
- Refund admin-driven.
- Shop wallet ledger: `SALE_CREDIT`, `REFUND_DEBIT`, `PAYOUT_DEBIT`, `ADJUSTMENT`.

### 5.3 Exception pattern

| Exception | HTTP (middleware) |
|-----------|-------------------|
| BadRequestException | 400 |
| NotFoundException | 404 |
| UnauthorizedException | 401 |
| ForbiddenException | 403 |
| Khác | 500 (message chung) |

Lưu ý: `AuthService` còn dùng `Exception` thường; một số service ném `UnauthorizedAccessException` → thành 500.

---

## 6. Web layer (`MiniLogistics.Web`)

### 6.1 Đã có UI (customer)

| Route | Chức năng |
|-------|-----------|
| `/`, `/products`, `/products/{id}` | Catalog |
| `/cart`, `/checkout` | Giỏ & checkout COD |
| `/orders`, `/orders/{id}` | Đơn hàng |
| `/addresses`, `/profile` | Địa chỉ & hồ sơ (+ avatar) |
| `/reviews` | Đánh giá của tôi |
| `/dashboard` | Dashboard customer |
| `/login`, `/register` | Auth |

Client services: Auth, Product, Cart, Address, Order, User, Review.

### 6.2 Chưa / thiếu trên Web

- Seller / Admin / Shipper portal hoàn toàn.
- Voucher trên checkout, payment online, returns, disputes, support, shipment tracking UI.
- Forgot-password page (link có, page chưa).
- Google login: nút / script còn stub.
- Token refresh dù API hỗ trợ.
- `AuthorizeRouteView` / role-based routing.
- Components dùng chung (page đang monolithic).

### 6.3 Auth phía client

- JWT lưu `localStorage` (`TokenStorageService`).
- Bearer gắn thủ công từng service — chưa có `DelegatingHandler`.
- `CustomerLayout` coi “có token” = đã đăng nhập (không parse expiry).

---

## 7. Điểm mạnh hiện tại

1. Domain marketplace khá đầy đủ trên API/BLL (không chỉ CRUD sản phẩm).
2. Layer rõ ràng: API / BLL / DAL / Web.
3. UoW + transaction cho order / voucher.
4. Global exception + request logging middleware.
5. Inventory tách riêng (reserved quantity) — nền tảng tốt cho concurrency.
6. Multi-role + refresh session đã thiết kế sẵn.
7. Swagger + JWT scheme phục vụ dev/Swagger test.

---

## 8. Rủi ro / nợ kỹ thuật (tóm tắt)

| Mức | Vấn đề |
|-----|--------|
| Cao | Secrets (DB password, JWT key) commit trong `appsettings.json` |
| Cao | `AccessTokenMinutes = 9999` — access token sống quá lâu |
| Cao | Forgot-password trả `resetToken` trong response (dev leak) |
| Cao | Test/Exception controllers không gắn môi trường Development |
| Trung | Web chỉ cover ~1/4 bề mặt API |
| Trung | Inventory logic trùng giữa Order và InventoryService |
| Trung | Không có project test |
| Trung | `PaymentService` phân trang/filter in-memory |
| Thấp | `CreateAddressDto.cs.cs` (tên file lỗi) |
| Thấp | Auth/Address bypass UoW (dùng `AppDbContext` trực tiếp) |
| Thấp | Avatar upload chưa chắc ghi `User.AvatarUrl` |

Chi tiết ưu tiên sửa & nâng cấp: xem [OPTIMIZATION_ROADMAP.md](OPTIMIZATION_ROADMAP.md).
