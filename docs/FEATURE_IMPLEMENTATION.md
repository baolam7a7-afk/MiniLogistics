# MiniLogistics — Feature Implementation Notes

## Đã triển khai (giữ kiến trúc API / BLL / DAL / Web)

### 1. Google Login / Register
- Backend: `AuthService.GoogleLoginAsync` — tạo mới hoặc **link theo email** nếu đã có tài khoản local.
- Frontend: GIS trên `/login` (`google-auth.js` + nút Google).
- Config: `MiniLogistics.Web/wwwroot/appsettings.json` → `Google:ClientId` (cùng ClientId API).
- Cần thêm origin `http://localhost:5107` trong Google Cloud Console Authorized JavaScript origins.

### 2. Ảnh sản phẩm
- Seeder cập nhật URL Unsplash theo từng sản phẩm (800×800, `fit=crop`).
- Khởi động API sẽ refresh ảnh picsum/placeholder cũ.

### 3. Voucher
- Bảng mới `user_vouchers` (nhận / đã dùng).
- API: `GET/POST api/user-vouchers/*`
- UI customer: `/vouchers`
- Admin/Seller tạo voucher: API sẵn `api/vouchers` (CRUD theo role).
- Apply lúc thanh toán: `api/order-vouchers` (đã có) + đánh dấu `user_vouchers.used`.
- Seed demo: `WELCOME10`, `FREESHIP50K`, `SHOP15OFF`.

### 4. Chat User ↔ Seller
- Bảng `conversations`, `chat_messages`.
- API: `api/chat/*` + SignalR hub `/hubs/chat`.
- UI: nút **Chat với Seller** trên Product Detail → `/chat/{id}`.

### 5. Thanh toán QR
- Order nhận `paymentMethod: qr`.
- API: `api/payments/qr/*` (tạo QR VietQR, check status, confirm demo).
- UI Checkout chọn QR → `/orders/{id}/qr-payment`.
- Config `VietQr` trong `appsettings.json` (đổi STK thật khi cần).

### 6. Migration
`AddChatUserVoucherQrPayment` — chạy khi API start (`MigrateAsync`) hoặc:
```bash
dotnet ef database update --project MiniLogistics.DAL --startup-project MiniLogistics.API
```

### 7. Phân quyền (tóm tắt)
| Role | Khả năng mới |
|------|----------------|
| customer | Google, nhận voucher, chat seller, QR |
| seller | voucher shop (API), chat khách, đơn shop |
| admin | voucher platform (API), confirm QR, quản trị hiện có |

## Còn có thể mở rộng
- Portal UI Seller/Admin (CRUD voucher/sản phẩm trên Web).
- SignalR realtime đầy đủ phía Blazor (hiện chat poll/reload sau gửi).
- Webhook ngân hàng thật thay nút confirm demo.
