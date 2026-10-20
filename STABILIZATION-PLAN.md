# SmartTourGuide: Kế hoạch ổn định hóa

> Đây là bản chuẩn bị ổn định hóa, chưa phải bản đã được xác nhận build/test. Môi trường rà soát không có .NET SDK, nên cần chạy các lệnh kiểm tra trên máy Windows của nhóm.

## Nguyên tắc

1. Giữ bản ZIP gốc làm bản dự phòng; chỉ sửa trong thư mục giải nén mới.
2. Chưa thêm tính năng mới cho đến khi build, khởi động, database và luồng QR chạy ổn định.
3. Mỗi lần chỉ sửa một nhóm lỗi, sau đó build và thử lại để biết thay đổi nào gây lỗi.
4. Không commit JWT key, mật khẩu database hoặc API key vào Git.

## Việc đã làm trong bản này

- Sửa thứ tự khởi tạo QR trong `SmartTourGuide.API/wwwroot/index.html`: mã tự tạo QR chờ sự kiện `DOMContentLoaded`, để script QR có `defer` cơ hội tải xong trước đó.
- Bỏ `.git`, file `.user` và log khởi động không chứa lỗi thực tế khỏi gói chuẩn bị. Không thay đổi logic backend trong bước này.

## Các vấn đề đã phát hiện cần xử lý tiếp

### P0 — Cấu hình chạy sạch

- `appsettings.json` để `Jwt:Key` trống có chủ đích. `Program.cs` yêu cầu key tối thiểu 32 ký tự. Cần lưu key bằng .NET User Secrets trên từng máy dev; tuyệt đối không ghi key thật vào `appsettings.json` rồi commit.
- Connection string hiện dùng SQL Server LocalDB, phù hợp với Windows cài SQL Server LocalDB; không phải cấu hình dùng chung cho Linux/container.

### P1 — Kiểm tra luồng QR

- QR demo `VNTQ-DEMO-2026` là `Reusable` và được seeder tạo khi chạy môi trường Development.
- QR URL lấy từ `location.origin`. Để quét bằng điện thoại, trang phải được mở bằng địa chỉ IPv4 LAN của laptop, không phải `localhost`, và API phải lắng nghe trên LAN profile.
- Logic `SingleUse` hiện vô hiệu hóa mã trong chính request GET đầu tiên. Nếu giao diện gọi lại endpoint khi đổi ngôn ngữ, lần gọi tiếp theo có thể bị từ chối. Không dùng SingleUse cho trình diễn nhiều thao tác cho đến khi thiết kế session/token cho luồng này.
- QR code được tải từ CDN; khi CDN hoặc Internet không truy cập được, thư viện không có sẵn và QR sẽ không sinh. Đây là phụ thuộc cần xử lý nếu yêu cầu chạy offline.

### P1 — Tự động kiểm tra

- `SmartTourGuide.Tests` hiện chưa có test `.cs`. `dotnet test` chưa chứng minh được các luồng nghiệp vụ chạy đúng nếu chưa bổ sung test.
- Cần thêm test tối thiểu cho đăng ký hợp lệ, email trùng, đăng nhập sai/đúng, mã QR hết hạn/vô hiệu hóa, và đổi ngôn ngữ.

### P2 — Tính tái lập

- Một số package tham chiếu dùng phiên bản wildcard `10.0.*`. Sau khi build xác nhận trên máy dev, nên khóa các phiên bản NuGet tương thích để các máy trong nhóm restore cùng một bộ dependency.
- Chỉ bổ sung controller cho các entity/session còn thiếu sau khi đối chiếu yêu cầu đồ án; không tạo API chỉ vì thấy entity.

## Trình tự kiểm tra trên Windows

Chạy PowerShell tại thư mục chứa `SmartTourGuide.sln`.

### 1. Kiểm tra JWT key cho máy dev

Nếu API hiện đã khởi động được, chưa cần đổi secret. Nếu gặp lỗi `JWT signing key was not configured` hoặc key ngắn hơn 32 ký tự, đặt một key ngẫu nhiên riêng bằng .NET User Secrets:

```powershell
dotnet user-secrets set "Jwt:Key" "THAY_BANG_CHUOI_NGAU_NHIEN_DAI_HON_32_KY_TU" --project .\SmartTourGuide.API\SmartTourGuide.API.csproj
```

Không dùng nguyên chuỗi ví dụ trên cho môi trường thật.

### 2. Restore, build và test

```powershell
dotnet restore .\SmartTourGuide.sln
dotnet build .\SmartTourGuide.sln --no-restore
dotnet test .\SmartTourGuide.sln --no-build
```

Ghi lại nguyên văn lỗi đầu tiên nếu một lệnh thất bại. Đừng sửa nhiều file cùng lúc.

### 3. Chạy API và kiểm tra trên laptop

```powershell
dotnet run --project .\SmartTourGuide.API\SmartTourGuide.API.csproj --launch-profile http
```

Mở lần lượt:

- `http://localhost:5011/health/live` — phải trả trạng thái khỏe.
- `http://localhost:5011/openapi/v1.json` — phải trả JSON OpenAPI.
- `http://localhost:5011/` — bấm tạo QR, QR demo phải hiện.
- `http://localhost:5011/api/public/tours/qr/VNTQ-DEMO-2026?language=vi` — phải trả JSON tour.

### 4. Thử trên điện thoại

1. Điện thoại và laptop phải cùng Wi-Fi.
2. Tìm IPv4 của laptop bằng `ipconfig`.
3. Chạy `dotnet run --project .\SmartTourGuide.API\SmartTourGuide.API.csproj --launch-profile lan`.
4. Mở `http://IPV4_CUA_LAPTOP:5011/` ngay trên laptop, thay bằng IP thật.
5. Tạo QR từ trang này rồi quét điện thoại. Nếu không kết nối được, kiểm tra Windows Firewall cho cổng 5011.

## Tiêu chí coi là ổn định bước đầu

- Restore/build không lỗi.
- API khởi động sau khi cấu hình secret và database đúng.
- Migration/seed chạy được trên DB dev.
- Health check, OpenAPI, API tour và UI QR hoạt động trên laptop.
- QR dùng được từ điện thoại cùng Wi-Fi.
- Có bộ smoke test và test nghiệp vụ trước khi nhận thêm tính năng.
