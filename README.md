# MiniLogistics

Nền tảng marketplace kết hợp vận chuyển: khách mua hàng, người bán quản lý shop, shipper giao hàng và admin điều hành hệ thống.

Demo: http://minilogistics.somee.com

## Công nghệ

| Thành phần | Project | Công nghệ |
|---|---|---|
| API | `MiniLogistics.API` | ASP.NET Core, JWT, SignalR, .NET 10 |
| Nghiệp vụ | `MiniLogistics.BLL` | Services, DTO, BCrypt |
| Dữ liệu | `MiniLogistics.DAL` | EF Core, SQL Server |
| Giao diện | `MiniLogistics.Web` | Blazor WebAssembly |

Solution: `LogisticsShop.slnx`

## Vai trò

- **Khách hàng:** danh mục, giỏ hàng, thanh toán, voucher, đơn hàng, đánh giá, chat với shop.
- **Người bán:** sản phẩm, kho, đơn hàng, voucher, doanh thu, chat với khách và admin.
- **Shipper:** nhận đơn và cập nhật trạng thái giao hàng.
- **Admin:** duyệt shop, ngừng bán, tài chính, người dùng và chat với seller.

## Chạy local

Cần .NET 10 SDK và SQL Server. Chuỗi kết nối nằm trong `MiniLogistics.API/appsettings.json`.

```bash
dotnet ef database update --project MiniLogistics.DAL --startup-project MiniLogistics.API
dotnet run --project MiniLogistics.API
dotnet run --project MiniLogistics.Web
```

- API: http://localhost:5136
- Web: http://localhost:5107
- Địa chỉ API của web: `MiniLogistics.Web/wwwroot/appsettings.json`

Khi chạy local, web và API là hai tiến trình riêng. Bản trên Somee gộp cả hai vào một website.
