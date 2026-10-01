# MiniLogistics

Nền tảng **marketplace logistics mini** (kiểu sàn thương mại điện tử + vận chuyển): khách mua hàng, seller quản lý shop, shipper giao hàng, admin điều hành hệ thống.

## Tech stack

| Layer | Project | Công nghệ |
|-------|---------|-----------|
| API | `MiniLogistics.API` | ASP.NET Core Web API, JWT, Swagger, .NET 10 |
| Business | `MiniLogistics.BLL` | Services + DTOs, BCrypt, Google Auth |
| Data | `MiniLogistics.DAL` | EF Core + SQL Server, Repository + Unit of Work |
| Client | `MiniLogistics.Web` | Blazor WebAssembly |

Solution: `LogisticsShop.slnx`

## Kiến trúc nhanh

```
Blazor WASM (Web)
       │  HTTP + JWT
       ▼
   ASP.NET API
       │
       ▼
     BLL (Services)
       │
       ▼
  DAL → SQL Server (LogisticsDb)
```

## Vai trò người dùng

- **customer** — duyệt sản phẩm, giỏ hàng, đặt COD, đơn hàng, địa chỉ, đánh giá
- **seller** — shop, sản phẩm, kho, voucher, đơn, ví, rút tiền *(API có sẵn; UI chưa có)*
- **shipper** — nhận / cập nhật shipment *(API có sẵn; UI chưa có)*
- **admin** — duyệt shop, user, dashboard, hoàn tiền, payout *(API có sẵn; UI chưa có)*

## Chạy dự án (local)

### Yêu cầu

- .NET 10 SDK
- SQL Server (local hoặc Docker) — connection trong `MiniLogistics.API/appsettings.json`

### Database

```bash
dotnet ef database update --project MiniLogistics.DAL --startup-project MiniLogistics.API
```

### API

```bash
dotnet run --project MiniLogistics.API
```

- HTTP: `http://localhost:5136`
- Swagger (Development): `/swagger`

### Web (Blazor)

```bash
dotnet run --project MiniLogistics.Web
```

- Mặc định: `http://localhost:5107` (CORS đã cấu hình origin này)
- API base URL hiện hard-code trong `MiniLogistics.Web/Program.cs`

## Tài liệu

| File | Nội dung |
|------|----------|
| [docs/PROJECT_OVERVIEW.md](docs/PROJECT_OVERVIEW.md) | Mô tả đầy đủ module, entity, API, UI |
| [docs/OPTIMIZATION_ROADMAP.md](docs/OPTIMIZATION_ROADMAP.md) | Gợi ý tối ưu & lộ trình nâng cấp |

## Trạng thái hiện tại (tóm tắt)

- Backend marketplace khá đầy đủ (~32 controllers, ~29 BLL services, 34 entities).
- Frontend mới cover **luồng customer** (catalog → cart → checkout COD → orders → profile).
- Chưa có test project, seller/admin/shipper UI, cổng thanh toán online.
- Secrets (JWT, DB password) đang nằm trong `appsettings.json` — cần chuyển sang User Secrets / env trước khi deploy.
