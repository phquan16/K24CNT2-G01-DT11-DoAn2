namespace FoodOrder.Models
{
    public class StatusConstants
    {
        public static class OrderStatus
        {
            public const string Pending = "Chờ thanh toán";
            public const string New = "new";
            public const string Cooking = "Đang chế biến";
            public const string Served = "Đã phục vụ";
            public const string Completed = "Hoàn thành";
            public const string Cancelled = "Hủy";
        }

        public static class ItemStatus
        {
            public const string New = "Mới";
            public const string Cooking = "Đang chế biến";
            public const string Ready = "Đã xong";
            public const string Served = "Đã phục vụ";
            public const string Cancelled = "Hủy";
        }
        public static class PaymentMethods
        {
            public const string Cash = "Tiền mặt";
            public const string VnPay = "VNPay";
        }
        public static class PaymentState
        {
            public const string Unpaid = "Chưa thanh toán";
            public const string Paid = "Đã thanh toán";
        }
        public static class TxnStatus
        {
            public const string Pending = "Chờ xử lý";
            public const string Success = "Thành công";
            public const string Failed = "Thất bại";
            public const string RefundNeeded = "Cần hoàn";
        }
    }
}
