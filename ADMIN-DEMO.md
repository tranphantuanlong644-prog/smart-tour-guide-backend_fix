# Admin Dashboard demo không cần đăng nhập

## Chạy trên laptop

Mở PowerShell tại thư mục project và chạy:

```powershell
dotnet run --project .\SmartTourGuide.API\SmartTourGuide.API.csproj --launch-profile lan
```

Profile `lan` đặt `ASPNETCORE_ENVIRONMENT=Development`. Trên chính laptop đang chạy API, mở:

- Admin: http://localhost:5011/admin/
- Health: http://localhost:5011/health/ready

Trang Admin tự tải Tổng quan và Phiên khách gần đây mỗi 2 giây. Khi điện thoại đóng app mà không gửi được tín hiệu thoát, phiên chuyển sang không hoạt động sau 13 giây kể từ tín hiệu hoạt động cuối cùng.

## Giới hạn bảo mật

Không cần đăng nhập chỉ áp dụng cho hai endpoint đọc dashboard (`GET /api/admin/overview` và `GET /api/admin/sessions`) khi backend chạy trong `Development` và request đến từ loopback của chính máy (`localhost`/`127.0.0.1`/`::1`). Mở Admin qua địa chỉ IP LAN có thể không cho đọc dashboard; hãy dùng `http://localhost:5011/admin/` trên laptop.

Các API Admin còn lại vẫn yêu cầu role `Administrator`. Không dùng chế độ demo không đăng nhập trong môi trường Production.
