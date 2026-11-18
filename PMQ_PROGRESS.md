# PMQ_PROGRESS

Cập nhật lúc: 2026-11-11

## Nguồn chuẩn

- Database: `K24CNT2-G01-DT11` trên `LAPTOP-N8I0ERNO\SQLEXPRESS03`
- Không có file `.sql` trong repository. Schema được đọc từ SQL Server (bảng, check, FK, view, stored procedure).
- Không tạo lại database, không sửa cấu trúc SQL, không xóa dữ liệu.

## Đã có sẵn (giữ nguyên nghiệp vụ đang chạy)

- Model các bảng chính, Identity `ApplicationUser` (`FullName`, `IsActive`), role Admin / Waiter / Kitchen / Cashier.
- Đăng nhập nhân viên (`ViewModels/AccountController.cs`), cookie Identity.
- Admin danh mục, admin bàn + QR động (`QRCoder`).
- Thực đơn theo mã bàn và phiên `TableSessions` (cookie `fo_session`, hạn 4 giờ).
- User seed trong DB: `admin`, `waiter01`, `kitchen01`, `cashier01`.

## Lệch so với SQL (đang sửa)

- `ProductIngredient` chưa có khóa chính kép → EF không dùng được.
- `OrderStatusHistories` phải map bảng `OrderStatusHistory`.
- `RowVersion` phải là `timestamp` / `IsRowVersion`.
- `Product.ProductImage` đang 1-1 trong khi bảng `ProductImages` là 1-n (unique chỉ khi `IsMain = 1`).
- `MenuService` gọi `.Where` trên navigation 1-1 → không biên dịch.
- `ITableService` chưa đăng ký DI.
- View form admin tên `From.cshtml` nhưng action trả về `Form`.
- `Login` tìm `Views/Account/Login.cshtml` trong khi giao diện đang nằm ở `Index.cshtml`.

## Quy tắc nghiệp vụ lấy từ procedure/view

- Trạng thái món: `Mới` → `Đang chế biến` → `Đã xong` → `Đã phục vụ`, hoặc `Hủy` theo `sp_UpdateItemStatus` (BR-02). Khách chỉ hủy được món `Mới`.
- Hủy món hoàn kho sản phẩm (nếu `Stock` không null) và nguyên liệu (`sp_UpdateItemStatus`).
- Trạng thái đơn do `sp_RecalcOrderStatus`: `Hủy` / `Hoàn tất` / `Đã phục vụ` / `Đang chế biến` / `Mới`. Đơn `Chờ thanh toán` không tính theo món.
- Tiền mặt: thu qua `sp_ConfirmCashPayment`.
- VNPay: đơn `Chờ thanh toán`, hạn 15 phút (`sp_ExpirePendingOrders` hoàn kho).
- Bếp: `vw_KitchenQueue`. Phục vụ: `vw_ReadyToServe`. Báo cáo: `vw_RevenueByDay`, `vw_TopProducts`, `vw_PeakHours`, `vw_TableUsage`, `vw_LowStock`.

## Việc đang làm

1. Sửa DbContext + navigation ảnh + DI + view login/form.
2. Giỏ hàng, gọi món, trừ kho, đơn của bàn.
3. Bếp, phục vụ, thu ngân, hết hạn VNPay.
4. Admin món, nguyên liệu, báo cáo, đánh giá.

## Chưa làm

- Toàn bộ mục 2–4 ở trên.
- Chưa chạy `dotnet build` sau các sửa này.

## Bước tiếp theo

Sửa `FoodOrderContext`, `Product`, `MenuService`, `Program.cs`, rồi viết service đơn hàng.
