namespace FoodOrder.Models
{
    public class StatusConstants
    {
        // Trạng thái bàn (DiningTable.Status)
        public static class TableStatuses
        {
            public const string Empty    = "Empty";      // Bàn trống
            public const string Occupied = "Occupied";   // Có khách
            public const string Reserved = "Reserved";   // Đặt trước
            public const string Cleaning = "Cleaning";   // Đang dọn
        }

        // Trạng thái đơn hàng (Order.Status)
        public static class OrderStatuses
        {
            public const string New        = "New";        // Vừa tạo
            public const string Confirmed  = "Confirmed";  // Nhà bếp xác nhận
            public const string Preparing  = "Preparing";  // Đang chế biến
            public const string Ready      = "Ready";      // Sẵn sàng phục vụ
            public const string Served     = "Served";     // Đã phục vụ
            public const string Cancelled  = "Cancelled";  // Đã hủy
            public const string Completed  = "Completed";  // Hoàn tất & thanh toán
        }

        // Trạng thái từng món trong đơn (OrderDetail.ItemStatus)
        public static class ItemStatuses
        {
            public const string New       = "New";       // Vừa đặt
            public const string Confirmed = "Confirmed"; // Bếp nhận
            public const string Preparing = "Preparing"; // Đang làm
            public const string Done      = "Done";      // Xong, chờ phục vụ
            public const string Served    = "Served";    // Đã mang ra bàn
            public const string Cancelled = "Cancelled"; // Hủy
        }

        // Phương thức thanh toán (Order.PaymentMethod, Payment.PaymentMethod)
        public static class PaymentMethods
        {
            public const string Cash    = "Cash";    // Tiền mặt
            public const string VNPay   = "VNPay";   // VNPay
            public const string MoMo    = "MoMo";    // MoMo
            public const string ZaloPay = "ZaloPay"; // ZaloPay
        }

        // Trạng thái thanh toán của đơn (Order.PaymentStatus)
        public static class PaymentStatuses
        {
            public const string Unpaid   = "Unpaid";   // Chưa thanh toán
            public const string Partial  = "Partial";  // Thanh toán một phần
            public const string Paid     = "Paid";     // Đã thanh toán đủ
            public const string Refunded = "Refunded"; // Đã hoàn tiền
        }

        // Trạng thái giao dịch thanh toán (Payment.Status)
        public static class TransactionStatuses
        {
            public const string Pending   = "Pending";   // Chờ xử lý
            public const string Success   = "Success";   // Thành công
            public const string Failed    = "Failed";    // Thất bại
            public const string Cancelled = "Cancelled"; // Đã hủy
        }
    }
}
