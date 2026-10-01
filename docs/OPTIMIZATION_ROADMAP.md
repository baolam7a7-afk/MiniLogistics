# MiniLogistics — Lộ trình tối ưu & nâng cấp

Gợi ý ưu tiên theo ảnh hưởng thực tế (P0 = làm sớm, P3 = dài hạn). Không phải checklist bắt buộc — chọn theo mục tiêu học tập / demo / production.

---

## P0 — Bảo mật & ổn định (làm trước)

### 1. Đưa secrets ra khỏi repo

- Chuyển connection string, JWT key sang **User Secrets** / biến môi trường.
- Thêm `appsettings.json` mẫu không chứa password thật (ví dụ `appsettings.Example.json`).
- Rotate JWT key + DB password nếu repo từng public.

### 2. Siết JWT & auth

| Hiện tại | Đề xuất |
|----------|---------|
| Access token ~9999 phút | 15–60 phút; dựa vào refresh token |
| Forgot-password trả `resetToken` | Chỉ gửi email; không trả token JSON ngoài Development |
| `TestController`, `ExceptionTestController` | Bọc `if (env.IsDevelopment())` hoặc xóa khỏi release |
| Empty password hash cho Google user | Flag `AuthProvider` + guard rõ ràng mọi flow password |

### 3. Pipeline production tối thiểu

- Bật `UseHttpsRedirection` (và HSTS khi deploy).
- CORS chỉ origin thật; không `AllowAnyOrigin`.
- Rate limit cho `login` / `forgot-password` / `register`.
- Health check: `/health` (DB ping).

### 4. Bug / gap nghiệp vụ nên vá sớm

| Vấn đề | Hướng xử lý |
|--------|-------------|
| Checkout không clear cart sau tạo đơn | Clear cart trong `OrderService.CreateAsync` (cùng transaction) hoặc Web gọi clear sau success |
| Order reserve/release không qua `IInventoryService` | Gọi `InventoryService` trong transaction để tránh drift |
| Delivered/paid chưa chắc deduct inventory + credit wallet | Chuẩn hóa state machine: khi `delivered`/`paid` → deduct reserved + `SALE_CREDIT` |
| `UnauthorizedAccessException` → 500 | Map trong middleware hoặc đổi sang `ForbiddenException` |
| Auth ném `Exception` thường | Đổi sang `BadRequestException` / `UnauthorizedException` |

---

## P1 — Hoàn thiện trải nghiệm customer (Web)

### UX / auth

1. `DelegatingHandler` gắn Bearer tự động + 401 → gọi `api/auth/refresh` hoặc logout.
2. Trang forgot / reset / change password.
3. Hoàn thiện Google Sign-In (`google-auth.js` + nút Login).
4. Cấu hình API base URL qua `wwwroot/appsettings.json` (bỏ hard-code `localhost:5136`).
5. Parse JSON lỗi middleware để hiện message cho user (thay vì `null`/`false` im lặng).

### Checkout & catalog

1. Field voucher trên checkout (gọi `order-vouchers` sau khi tạo đơn, hoặc mở rộng API tạo đơn kèm code).
2. Review trên `ProductDetail` (đọc theo product + tạo sau khi đã mua).
3. Tách component dùng chung: product card, pagination, order status badge, loading/empty state.
4. `AuthorizeRouteView` / redirect tập trung thay vì chỉ check token trong layout.

---

## P2 — Mở rộng portal theo role

Backend đã có gần hết — ưu tiên UI theo business value:

| Phase | Portal | Module ưu tiên |
|-------|--------|----------------|
| 2.1 | **Seller** | Shop (create/pending), Product/Variant/Inventory, Orders status, Seller dashboard |
| 2.2 | **Seller finance** | Wallet, payout request, voucher |
| 2.3 | **Shipper** | Danh sách shipment được gán, cập nhật status + events |
| 2.4 | **Admin** | Duyệt shop, user lock/role, admin dashboard, refunds, disputes, categories |

Gợi ý kỹ thuật UI:

- Layout riêng: `SellerLayout`, `AdminLayout`, `ShipperLayout`.
- Guard theo claim `Role`.
- Có thể tách Blazor project theo portal nếu bundle WASM phình to — hoặc dùng lazy assemblies.

---

## P3 — Kiến trúc & chất lượng code

### Data / BLL

| Đề xuất | Lý do |
|---------|-------|
| Enum / value object cho status (Order, Payment, Shipment, Shop) | Tránh stringly-typed, dễ validate transition |
| `IEntityTypeConfiguration<T>` tách khỏi `AppDbContext` | DbContext gọn, dễ maintain |
| Đưa `PasswordResetToken` + Auth/Address vào UoW | Một lối data access |
| Shared contracts project (`MiniLogistics.Contracts`) | Web + API dùng chung DTO, hết drift model |
| Query paging DB-side cho Payment (và chỗ load-all tương tự) | Scale khi data lớn |
| Index `GoogleId`, giới hạn `nvarchar` thay vì `max` | Query & storage |
| Concurrency token trên `Inventory` (`RowVersion`) | Tránh oversell khi concurrent order |

### API

| Đề xuất | Lý do |
|---------|-------|
| Assembly scanning / extension `AddApplicationServices()` | `Program.cs` đang đăng ký thủ công rất dài |
| Versioning (`/api/v1`) | Chuẩn bị khi breaking change |
| ProblemDetails (RFC 7807) thống nhất error body | Thay anonymous object + `ErrorResponseDTO` lệch nhau |
| File storage abstraction (local → blob) | Avatar/product image production-ready |
| Magic-byte validate upload + giới hạn size | An toàn hơn chỉ check extension |
| AvatarController cập nhật `User.AvatarUrl` | Đồng bộ profile |

### Testing

Thêm solution projects:

```
MiniLogistics.BLL.Tests      # Unit: Order, Inventory, Auth, Cart
MiniLogistics.API.Tests      # Integration: WebApplicationFactory + Testcontainers SQL
MiniLogistics.Web.Tests      # bUnit cho component quan trọng
```

Ưu tiên test trước: tạo đơn + reserve stock, cancel release, voucher apply, auth refresh.

### DevOps

- CI: `dotnet build` + `dotnet test` trên PR.
- Docker Compose: API + SQL Server (+ optional Web static serve).
- `.sln` chính thức (ngoài `.slnx`) nếu team dùng tooling cũ.
- Không commit `wwwroot/uploads` thật — gitignore uploads.

---

## Nâng cấp tính năng sản phẩm (roadmap sản phẩm)

Thứ tự gợi ý nếu muốn tiến gần “sàn thật”:

1. **Thanh toán online** — VNPay / MoMo / Stripe webhook; tách `PaymentGateway` adapter; hiện chỉ COD + admin đổi status.
2. **Đơn đa shop** — hiện 1 order = 1 shop; split cart theo shop hoặc multi-order checkout.
3. **Realtime** — SignalR: cập nhật trạng thái đơn / shipment cho customer & shipper.
4. **Search** — full-text / Elastic hoặc SQL FTS cho product.
5. **Notification** — email (reset password thật), in-app notification.
6. **Audit log** — hành động admin/seller nhạy cảm (duyệt shop, payout, refund).
7. **Reporting** — tận dụng `ReportSnapshot` + job nền (Hangfire / BackgroundService) thay vì chỉ CRUD snapshot.
8. **i18n** — nếu mở rộng user quốc tế (hiện message tiếng Việt cứng).

---

## Ma trận ưu tiên nhanh

| Việc | Effort | Impact | Khi nào |
|------|--------|--------|---------|
| Secrets + JWT TTL + ẩn test endpoints | Thấp | Rất cao | Ngay |
| Clear cart + unify inventory | Thấp–TB | Cao | Sprint gần |
| HttpClient handler + refresh | TB | Cao | Trước khi demo auth dài |
| Seller UI (core) | Cao | Rất cao | Khi cần demo full marketplace |
| Shared contracts + tests | TB–Cao | Cao | Trước refactor lớn |
| Online payment | Cao | Rất cao | Khi có yêu cầu thật |
| SignalR / search / multi-shop | Cao | Trung–Cao | Sau khi portal ổn |

---

## Đề xuất 3 phase thực thi

### Phase A — Harden (1–3 ngày)

Secrets, JWT, tắt debug endpoints, clear cart, unify inventory calls, map exceptions, HTTPS/CORS production checklist.

### Phase B — Customer polish + Seller MVP (1–2 tuần)

Auth UX (refresh, forgot password, Google), voucher checkout, review trên product; Seller: shop + products + orders + dashboard.

### Phase C — Platform (2–4+ tuần)

Admin/Shipper UI, tests + CI, payment gateway, concurrency inventory, shared contracts, Docker.

---

## Liên kết

- Tổng quan hệ thống: [PROJECT_OVERVIEW.md](PROJECT_OVERVIEW.md)
- README gốc: [../README.md](../README.md)
