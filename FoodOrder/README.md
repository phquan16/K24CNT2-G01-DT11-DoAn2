# FoodOrder — Hệ thống gọi món bằng mã QR cho nhà hàng

## 1. Tên dự án

**FoodOrder** — Ứng dụng web ASP.NET Core MVC quản lý gọi món và vận hành nhà hàng theo mô hình QR Menu.
Đồ án môn học: `K24CNT2-G01-DT11`.

## 2. Giới thiệu

FoodOrder số hóa toàn bộ quy trình phục vụ tại nhà hàng:

- Khách ngồi tại bàn quét mã QR đặt món, **không cần đăng nhập, không cần cài ứng dụng**.
- Đơn hàng đi thẳng xuống màn hình bếp, nhân viên phục vụ nhận món đã nấu xong, thu ngân thu tiền và chốt đơn.
- Quản trị viên quản lý thực đơn, danh mục, bàn & mã QR, nguyên liệu, nhân viên và theo dõi doanh thu.

Ứng dụng giữ nguyên cơ sở dữ liệu SQL Server hiện có (DB-first, không dùng migration), tận dụng stored procedure và view sẵn có để xử lý các quy tắc nghiệp vụ.

## 3. Mục tiêu

| Mục tiêu | Mô tả |
|---|---|
| Số hóa gọi món | Thay thực đơn giấy bằng mã QR riêng cho từng bàn, giỏ hàng trực tuyến. |
| Chính xác trạng thái | Mỗi món và mỗi đơn đi qua chuỗi trạng thái được kiểm soát chặt chẽ. |
| Điều phối bếp – phục vụ – thu ngân | Ba màn hình nghiệp vụ tách biệt, dữ liệu dùng chung theo thời gian thực. |
| Quản trị tập trung | Quản lý thực đơn, tồn kho, nhân viên và báo cáo doanh thu trên một giao diện. |
| An toàn dữ liệu | Phân quyền theo vai trò, chống giả mạo yêu cầu (CSRF), không tự ý thay đổi cấu trúc CSDL. |

## 4. Công nghệ sử dụng

| Thành phần | Công nghệ / phiên bản |
|---|---|
| Nền tảng | ASP.NET Core MVC, hướng đích `net8.0` |
| Ngôn ngữ | C# (nullable enabled, dùng chung `ImplicitUsings`) |
| Giao diện | Razor Views, Bootstrap 5, JavaScript thuần, jQuery Validation |
| ORM | Entity Framework Core 8 (`Microsoft.EntityFrameworkCore.SqlServer`) — mô hình DB-first, không migration |
| Cơ sở dữ liệu | Microsoft SQL Server (database `K24CNT2-G01-DT11`), stored procedure + view |
| Xác thực & phân quyền | ASP.NET Core Identity (Admin, Waiter, Kitchen, Cashier) |
| Thư viện hỗ trợ | QRCoder 1.6.0 (sinh mã QR), CsvHelper 33.1.0 (nhập thực đơn CSV), Swashbuckle 8.1.4 |
| Thanh toán trực tuyến | VNPay (tùy chọn, bật bằng cấu hình, ký HMAC-SHA512) |

## 5. Chức năng chính

### 5.1. Khách hàng (quét QR, không cần tài khoản)

- Xem thực đơn theo bàn: tìm kiếm, lọc danh mục, phân trang 12 món/trang, giá khuyến mãi.
- Xem chi tiết món: mô tả, độ cay, ảnh, tùy chọn (size, độ cay...), đánh giá gần nhất.
- Thêm vào giỏ: chọn số lượng 1–50, chọn tùy chọn bắt buộc theo từng nhóm, ghi chú món (tối đa 300 ký tự).
- Giỏ hàng: sửa số lượng, xóa món, xóa toàn bộ; cảnh báo món hết hàng/ngừng bán.
- Gửi đơn: chọn thanh toán tiền mặt tại quầy hoặc VNPay (khi đã cấu hình), nhập email (không bắt buộc) và ghi chú đơn.
- Mỗi lần gửi tạo **một đơn riêng** (không gộp đơn); không thể thêm món vào đơn đã gửi — muốn gọi thêm thì tạo đơn mới.
- "Đơn của tôi": theo dõi trạng thái từng món và cả đơn; hủy món khi món còn ở trạng thái `Mới`; hủy đơn đang `Chờ thanh toán`.
- Đánh giá món đã gọi trong phiên (1–5 sao, tên, nhận xét ≤ 500 ký tự), mỗi món chỉ đánh giá một lần trong một phiên bàn.
- Mã QR không hợp lệ/bàn ngừng hoạt động: trả về trang **404 – Không tìm thấy bàn**.

### 5.2. Quản trị viên (Admin)

- **Tổng quan**: doanh thu đã thanh toán, số đơn đã thanh toán, số đơn đang mở, số mặt hàng sắp hết, biểu đồ doanh thu theo ngày, top món bán chạy, giờ cao điểm, mức sử dụng bàn, cảnh báo tồn kho, đơn mới nhất.
- **Danh mục**: thêm/sửa, bật/tắt hiển thị, thứ tự sắp xếp.
- **Sản phẩm**: thêm/sửa, giá, giá khuyến mãi, tồn kho, trạng thái bán, ảnh (nhiều ảnh), tùy chọn (nhiều nhóm, giá cộng thêm), chống ghi đè đồng thời bằng `RowVersion`.
- **Bàn ăn & QR**: thêm/sửa bàn, khu vực, trạng thái (Trống / Đang phục vụ / Tạm ngưng), tải ảnh QR PNG, in hàng loạt QR cho các bàn đang hoạt động.
- **Nguyên liệu**: thêm/sửa, nhập thêm tồn kho (có kiểm soát `RowVersion`), công thức món (định lượng nguyên liệu theo món), cảnh báo nguyên liệu sắp hết.
- **Nhập món từ CSV**: tải tệp, xem trước và kiểm tra từng dòng, xác nhận nhập; tối đa 2.000 dòng/lần.
- **Đơn hàng**: lọc theo trạng thái, tìm theo mã đơn/tên bàn, phân trang; xem chi tiết món, lịch sử trạng thái, thanh toán; hủy đơn kèm lý do (bị chặn khi đơn đã thanh toán hoặc đã có món chế biến xong/phục vụ).
- **Nhân viên**: tạo tài khoản, sửa họ tên/email/vai trò, khóa/mở khóa, đặt lại mật khẩu; chặn tự khóa và tự bỏ quyền Admin của chính mình.
- **Báo cáo doanh thu**: chọn khoảng ngày (mặc định 7 ngày gần nhất, tối đa 366 ngày) và xem cùng bộ chỉ số như trang Tổng quan.

### 5.3. Bếp (Kitchen)

- Hàng đợi món chưa chế biến, sắp xếp theo thời gian gọi.
- Bắt đầu chế biến (`Mới → Đang chế biến`), báo xong (`Đang chế biến → Đã xong`).
- Hủy món kèm lý do (`Mới/Đang chế biến → Hủy`), hệ thống tự hoàn tồn kho sản phẩm và nguyên liệu.
- Danh sách món đã xong chờ phục vụ; trang tự làm mới mỗi 30 giây.

### 5.4. Phục vụ (Waiter)

- Sơ đồ bàn: trạng thái bàn, số đơn đang mở, số tiền chưa thu, số món đã xong.
- Đánh dấu món đã phục vụ (`Đã xong → Đã phục vụ`).
- Đóng bàn: chỉ thành công khi bàn không còn đơn chưa hoàn tất/chưa thanh toán; đóng toàn bộ phiên bàn đang mở.

### 5.5. Thu ngân (Cashier)

- Tổng hợp trong ngày: tổng tiền mặt đã thu, số hóa đơn tiền mặt, số đơn VNPay đang chờ.
- Danh sách hóa đơn tiền mặt chưa thu kèm chi tiết món; nút **Xác nhận đã thu tiền mặt**.
- Danh sách giao dịch ở trạng thái `Cần hoàn` để xử lý hoàn tiền theo quy trình của nhà hàng.

## 6. Cấu trúc thư mục

```text
FoodOrder/
├── Areas/
│   ├── Admin/                 # Khu vực quản trị (Controller + View)
│   ├── Cashier/               # Màn hình thu ngân
│   ├── Kitchen/               # Màn hình bếp
│   └── Waiter/                # Màn hình phục vụ
├── Controllers/
│   ├── AccountController.cs   # Đăng nhập / đăng xuất / từ chối truy cập
│   ├── TableContextController.cs # Xác thực mã bàn + phiên bàn dùng chung
│   ├── MenuController.cs      # Thực đơn và chi tiết món
│   ├── CartController.cs      # Giỏ hàng (session)
│   ├── CheckoutController.cs  # Gửi đơn
│   ├── OrdersController.cs    # Đơn của khách, hủy món/đơn, VNPay return
│   ├── ReviewController.cs    # Đánh giá món
│   └── HomeController.cs      # Trang giới thiệu / lỗi
├── Data/
│   └── FoodOrderContext.cs    # DbContext (DB-first, view không khóa)
├── Helpers/                   # SqlProc, VnPayHash, MoneyHelper, SlugHelper, RowVersionHelper
├── Models/                    # Entity + hằng số trạng thái + quy tắc trạng thái
├── Services/                  # Nghiệp vụ: đơn hàng, thanh toán, bếp, báo cáo...
├── ViewModels/                # DTO cho View (bind + validate)
├── Views/                     # Razor Views (khách + tài khoản + layout)
├── wwwroot/                   # CSS/JS/thư viện tĩnh/ảnh tải lên
├── appsettings.json           # Chuỗi kết nối + cấu hình
├── Program.cs                 # DI, Identity, Session, routing
└── FoodOrder.csproj
```

## 7. Yêu cầu môi trường

| Thành phần | Yêu cầu |
|---|---|
| Hệ điều hành | Windows 10/11 hoặc Windows Server (hướng dẫn dùng Windows Authentication cho SQL Server). |
| .NET SDK | .NET SDK 8.0 trở lên (dự án build thành công với SDK 8.x/9.x/10.x, mục tiêu `net8.0`). |
| SQL Server | SQL Server 2016+ / SQL Server Express, database `K24CNT2-G01-DT11`. |
| Công cụ | `dotnet` CLI; SQL Server Management Studio (SSMS) hoặc `sqlcmd`. |
| Trình duyệt | Chrome / Edge / Firefox phiên bản hiện đại. |
| Tùy chọn | Tài khoản VNPay Sandbox (TmnCode, HashSecret) nếu muốn bật thanh toán online. |

## 8. Cài đặt

1. **Lấy mã nguồn** về máy và mở thư mục dự án:

   ```bash
   cd D:\K24CNT2\K24CNT2-G01-DT11-DoAn2\FoodOrder
   ```

2. **Khôi phục gói NuGet và build**:

   ```bash
   dotnet restore
   dotnet build
   ```

3. **Chuẩn bị cơ sở dữ liệu** (thực hiện một lần trên database mới):

   | Thứ tự | Tệp script | Nội dung |
   |---|---|---|
   | 1 | `E:\SQLbang1.sql` | Tạo bảng, view (`vw_KitchenQueue`, `vw_ReadyToServe`, `vw_RevenueByDay`, `vw_PeakHours`, `vw_TopProducts`, `vw_TableUsage`, `vw_LowStock`) và stored procedure (`sp_UpdateItemStatus`, `sp_RecalcOrderStatus`, `sp_ConfirmCashPayment`, `sp_ExpirePendingOrders`). |
   | 2 | `E:\SQLbang2.sql` | Nạp vai trò, tài khoản nhân viên mẫu và dữ liệu thực đơn/bàn mẫu. |
   | 3 | `E:\SQLbang4.sql` | Bổ sung 4 bảng Identity còn thiếu (`AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`) nếu database chưa có. Script kiểm tra tồn tại nên chạy lại an toàn. |
   | 4 (tùy chọn) | `E:\SQLbang3.sql` | Kiểm tra đối chiếu dữ liệu và thử nghiệp vụ; kết thúc bằng `ROLLBACK`, không thay đổi dữ liệu. |

   > `SQLbang1.sql` tạo bảng không kèm điều kiện "nếu chưa tồn tại", vì vậy chỉ chạy trên database trống. Nếu database `K24CNT2-G01-DT11` đã có bảng, **không chạy lại** script này.
   >
   > Trước khi chạy `SQLbang1.sql`, nên tạo sẵn database `K24CNT2-G01-DT11` (collation `Vietnamese_CI_AS`) bằng SSMS hoặc:
   >
   > ```sql
   > CREATE DATABASE [K24CNT2-G01-DT11] COLLATE Vietnamese_CI_AS;
   > ```

4. **Cấu hình chuỗi kết nối** (mục 9) rồi chạy ứng dụng (mục 10).

## 9. Cấu hình SQL Server

Mở `appsettings.json`, cập nhật `ConnectionStrings:Default` theo máy của bạn:

```json
{
  "ConnectionStrings": {
    "Default": "Server=<TÊN_MÁY>\\<TÊN_INSTANCE>; Database=K24CNT2-G01-DT11; Trusted_Connection=True; TrustServerCertificate=True"
  }
}
```

Ghi chú:

- `Trusted_Connection=True` dùng Windows Authentication — tài khoản chạy ứng dụng cần quyền truy cập database.
- Nếu dùng SQL Authentication, thay bằng `User Id=...; Password=...;` và **không** đưa mật khẩu thật lên kho mã nguồn; nên dùng biến môi trường hoặc User Secrets.
- Ứng dụng không chạy migration và không tự tạo/sửa bảng; toàn bộ cấu trúc do các script SQL quyết định.

### Cấu hình VNPay (tùy chọn)

Thanh toán online chỉ hoạt động khi cung cấp đủ ba khóa cấu hình:

```json
{
  "VnPay": {
    "TmnCode": "<mã website VNPay>",
    "HashSecret": "<chuỗi bí mật>",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
  }
}
```

Ưu tiên đặt qua biến môi trường (`VnPay__TmnCode`, `VnPay__HashSecret`, `VnPay__BaseUrl`) hoặc User Secrets. Khi chưa cấu hình, hệ thống **tắt lựa chọn VNPay** và chỉ cho thanh toán tiền mặt tại quầy.

## 10. Chạy chương trình

```bash
dotnet run --launch-profile http
```

Ứng dụng lắng nghe tại:

- HTTP: `http://localhost:5155`
- HTTPS (profile `https`): `https://localhost:7050` và `http://localhost:5155`

Hoặc chỉ định cổng trực tiếp:

```bash
dotnet run --urls "http://localhost:5155"
```

Khi chạy ở môi trường Development, lỗi chi tiết được hiển thị trên trang lỗi; ở Production ứng dụng dùng `/Home/Error` và HSTS.

## 11. Hướng dẫn sử dụng

### 11.1. Khách hàng

1. Quét mã QR tại bàn (hoặc mở `http://<host>/Menu?table=<MãBàn>`).
2. Chọn món → chọn tùy chọn/số lượng/ghi chú → **Thêm vào giỏ**.
3. Mở **Giỏ hàng**, kiểm tra món và tổng tiền → **Tiến hành đặt món**.
4. Chọn phương thức thanh toán, nhập email (nếu muốn nhận thông tin) → **Gửi đơn**.
5. Theo dõi tại **Đơn của tôi**: trạng thái từng món, hủy món khi còn `Mới`, đánh giá món đã dùng.
6. Thanh toán tại quầy khi dùng xong (tiền mặt) hoặc thanh toán VNPay nếu được bật.

### 11.2. Quản trị viên

1. Đăng nhập, hệ thống chuyển đến `/Admin`.
2. Tạo **Danh mục** → thêm **Sản phẩm** (kèm ảnh, tùy chọn, giá, tồn kho).
3. Tạo **Bàn ăn**, tải/in mã QR tại mục **Bàn ăn & QR**.
4. Khai báo **Nguyên liệu** và công thức món để cảnh báo tồn kho.
5. Quản lý **Nhân viên**: tạo tài khoản, gán vai trò, khóa/mở khóa, đặt lại mật khẩu.
6. Theo dõi **Đơn hàng** và **Báo cáo doanh thu**.

### 11.3. Bếp

1. Đăng nhập → tự chuyển tới `/Kitchen/Queue`.
2. Bấm **Bắt đầu** khi nhận món; bấm **Đã xong** khi hoàn tất.
3. Nếu hết nguyên liệu, bấm **Hủy** — hệ thống hoàn tồn kho tương ứng.

### 11.4. Phục vụ

1. Đăng nhập → `/Waiter`.
2. Theo dõi món đã xong và bấm **Phục vụ** khi mang món tới bàn.
3. Khi khách dùng xong và đã thanh toán, bấm **Đóng bàn**.

### 11.5. Thu ngân

1. Đăng nhập → `/Cashier`.
2. Mở hóa đơn tiền mặt chưa thu, đối chiếu món và số tiền.
3. Bấm **Xác nhận đã thu tiền mặt** — đơn chuyển `Hoàn tất` / `Đã thanh toán`.
4. Theo dõi danh sách giao dịch `Cần hoàn` (nếu có) để hoàn tiền thủ công.

## 12. Tài khoản và phân quyền

| Vai trò | Tên đăng nhập (dữ liệu mẫu) | Trang mặc định sau đăng nhập | Quyền chính |
|---|---|---|---|
| Admin | `admin` | `/Admin` | Toàn quyền quản trị, xem báo cáo, quản lý nhân viên. |
| Waiter | `waiter01` | `/Waiter` | Sơ đồ bàn, phục vụ món, đóng bàn. |
| Kitchen | `kitchen01` | `/Kitchen/Queue` | Hàng đợi bếp, cập nhật trạng thái món. |
| Cashier | `cashier01` | `/Cashier` | Thu tiền mặt, theo dõi VNPay, danh sách cần hoàn. |

- Mật khẩu của các tài khoản mẫu được đặt trong `E:\SQLbang2.sql` (script seed). README không ghi mật khẩu; hãy đổi mật khẩu ngay sau khi triển khai bằng chức năng **Nhân viên → Đặt lại mật khẩu**.
- Khách hàng truy cập thực đơn **không cần tài khoản**; quyền truy cập được xác định bằng mã bàn hợp lệ và phiên bàn (`fo_session`, hiệu lực 4 giờ).
- Chính sách đăng nhập: mật khẩu tối thiểu 6 ký tự; khóa 5 phút sau 5 lần sai; cookie đăng nhập 8 giờ, tự gia hạn khi hoạt động; tài khoản bị khóa (`IsActive = false`) bị đăng xuất ngay ở lần kiểm tra kế tiếp.

## 13. Luồng đặt món bằng QR

```text
Admin tạo bàn → in QR (URL: {host}/Menu?table={MãBàn})
        │
Khách quét QR ──► MenuController.ResolveAsync
        │              ├─ Bàn không tồn tại / ngừng hoạt động / tạm ngưng → 404
        │              └─ Hợp lệ → tạo hoặc tái sử dụng phiên bàn (cookie fo_session)
        ▼
Xem thực đơn (tìm kiếm, lọc danh mục, phân trang)
        ▼
Chi tiết món (tùy chọn bắt buộc theo nhóm, số lượng 1–50, ghi chú ≤ 300 ký tự)
        ▼
Giỏ hàng (session) ──► Checkout
        ▼
Gửi đơn (tiền mặt hoặc VNPay) ──► Tạo ĐƠN MỚI + chi tiết món
        │                            trừ tồn kho sản phẩm/nguyên liệu
        ▼
"Đơn của tôi" (chỉ đơn thuộc phiên bàn hiện tại)
```

Đặc điểm:

- Mỗi lần gửi là một đơn độc lập; món đã gửi không sửa được, chỉ có thể hủy theo quy tắc trạng thái.
- Mã QR mã hóa đúng host đang truy cập khi tạo ảnh — khi triển khai phải tạo QR từ tên miền công khai, không dùng `localhost`.
- Giỏ hàng lưu trong Session (30 phút không hoạt động sẽ mất); phiên bàn lưu trong cookie `fo_session`.
- Không thể xem/sửa đơn của phiên bàn khác (mọi truy vấn đều ràng buộc `TableSessionId`).

## 14. Luồng xử lý đơn hàng

### 14.1. Trạng thái món

| Từ | Sang | Ai thực hiện | Điều kiện |
|---|---|---|---|
| Mới | Đang chế biến | Bếp | — |
| Mới | Hủy | Khách / nhân viên | Đơn chưa thanh toán |
| Đang chế biến | Đã xong | Bếp | — |
| Đang chế biến | Hủy | Nhân viên (bếp/admin) | Kèm lý do; hoàn tồn kho |
| Đã xong | Đã phục vụ | Phục vụ | — |
| Đã phục vụ | — | — | Không thể hủy/đổi ngược |

Mọi thay đổi trạng thái món đều đi qua `dbo.sp_UpdateItemStatus`, ghi lịch sử vào `OrderStatusHistory` và tự tính lại trạng thái đơn bằng `dbo.sp_RecalcOrderStatus`.

### 14.2. Trạng thái đơn

| Trạng thái | Ý nghĩa |
|---|---|
| Chờ thanh toán | Đơn VNPay vừa tạo, chưa thanh toán; tự hủy sau 15 phút. |
| Mới | Đơn tiền mặt vừa gửi, chưa có món nào được bếp nhận. |
| Đang chế biến | Có ít nhất một món đang chế biến. |
| Đã phục vụ | Toàn bộ món còn hiệu lực đã được phục vụ. |
| Hoàn tất | Đơn đã thanh toán và đã phục vụ xong. |
| Hủy | Đơn bị hủy; tổng tiền đưa về 0, tồn kho được hoàn. |

### 14.3. Quy tắc hủy

- Khách chỉ hủy được **món ở trạng thái Mới** và **đơn đang Chờ thanh toán**.
- Nhân viên hủy được món `Mới` hoặc `Đang chế biến` (kèm lý do, tối đa 300 ký tự).
- Không hủy món `Đã xong`/`Đã phục vụ`; đơn đã thanh toán **không thể hủy** — thu ngân xử lý hoàn tiền theo quy trình.
- Hủy món/đơn tự động hoàn tồn kho sản phẩm và nguyên liệu theo công thức.

## 15. Luồng thanh toán

### 15.1. Tiền mặt tại quầy

```text
Khách gửi đơn (Tiền mặt) → Bếp chế biến → Phục vụ → Khách dùng xong
        ▼
Thu ngân mở hóa đơn → "Xác nhận đã thu tiền mặt"
        ▼
sp_ConfirmCashPayment: ghi Payment (Thành công) + Đơn → Hoàn tất / Đã thanh toán
```

### 15.2. VNPay (khi đã cấu hình)

```text
Khách chọn VNPay → Tạo đơn "Chờ thanh toán" (hạn 15 phút)
        ▼
Chuyển tới cổng VNPay (URL ký HMAC-SHA512)
        ▼
VNPay gọi về /Orders/VnPayReturn
        ├─ Kiểm tra chữ ký (so sánh an toàn thời gian)
        ├─ Kiểm tra số tiền = TotalAmount × 100
        └─ Thành công → đơn Đã thanh toán; thất bại → giữ chờ/hủy theo mã lỗi
        ▼
ExpireOrdersWorker (mỗi phút): đơn quá hạn → Hủy + hoàn tồn kho
```

- Chưa cấu hình VNPay: nút thanh toán online bị vô hiệu, hệ thống chỉ nhận tiền mặt. Không có chức năng giả lập thanh toán trong giao diện.
- Sai chữ ký/sai số tiền/thiếu tham số: từ chối và hiển thị thông báo, không đổi trạng thái đơn.
- Hệ thống không tự hoàn tiền trực tuyến; giao dịch cần hoàn hiển thị ở màn hình thu ngân để xử lý thủ công.

## 16. Hình ảnh minh họa

Dự án chưa kèm ảnh chụp màn hình. Các hình ảnh được sinh khi chạy:

- **Mã QR bàn**: `/Admin/Tables/Qr?id={TableId}` (ảnh PNG) và `/Admin/Tables/PrintQr` (trang in).
- **Ảnh món**: quản trị viên tải lên tại mục Sản phẩm, lưu trong `wwwroot`.

Khi bổ sung ảnh minh họa, đặt trong thư mục `docs/images/` và chèn vào README bằng đường dẫn tương đối.

## 17. Ghi chú triển khai

- **HTTPS**: bật chứng chỉ TLS; ứng dụng dùng `UseHttpsRedirection` và HSTS ở Production.
- **QR và tên miền**: mã QR lấy `scheme://host` từ yêu cầu hiện tại. Hãy tạo/in QR khi truy cập bằng tên miền công khai để khách quét được.
- **Session**: giỏ hàng và dữ liệu tạm dùng bộ nhớ trong (`AddDistributedMemoryCache`). Nếu chạy nhiều máy chủ, cần Redis/SQL Server cache và sticky session.
- **Tác vụ nền**: `ExpireOrdersWorker` chạy trong tiến trình; khi mở rộng nhiều instance nên chuyển sang job riêng để tránh chạy trùng.
- **Cơ sở dữ liệu**: DB-first, không migration. Mọi thay đổi cấu trúc phải thực hiện bằng script và được phê duyệt trước; sao lưu định kỳ.
- **Bảo mật**: mọi POST đều có `ValidateAntiForgeryToken`; phân quyền bằng `[Authorize(Roles=...)]`; truy vấn qua EF Core (tham số hóa) và stored procedure; không lưu mật khẩu/khóa bí mật trong mã nguồn.
- **Tài khoản mẫu**: đổi mật khẩu và khóa các tài khoản không dùng trước khi chạy thật.
- **Nhật ký**: dùng `Logging` mặc định của ASP.NET Core; khi triển khai nên ghi log ra file/dịch vụ tập trung.

## 18. Hướng phát triển

- Thông báo thời gian thực (SignalR) cho bếp/phục vụ thay vì làm mới trang.
- Quản lý nhiều chi nhánh, nhiều khu vực và bàn theo sơ đồ kéo–thả.
- Chương trình khuyến mãi, mã giảm giá, tích điểm khách hàng thân thiết.
- Báo cáo nâng cao: lợi nhuận theo nguyên liệu, so sánh kỳ, xuất Excel/PDF.
- Bổ sung quy trình hoàn tiền VNPay tự động và đối soát giao dịch.
- Ứng dụng PWA cho khách và màn hình bếp chuyên dụng.
- Bộ kiểm thử tự động (unit test cho quy tắc trạng thái, integration test cho luồng đặt món/thanh toán).
