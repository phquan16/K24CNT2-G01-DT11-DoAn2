namespace FoodOrder.Models;

public static class ItemStatus
{
    public const string New = "Mới";
    public const string Cooking = "Đang chế biến";
    public const string Done = "Đã xong";
    public const string Served = "Đã phục vụ";
    public const string Cancelled = "Hủy";
}

public static class OrderStatus
{
    public const string New = "Mới";
    public const string Cooking = "Đang chế biến";
    public const string Served = "Đã phục vụ";
    public const string Completed = "Hoàn tất";
    public const string Cancelled = "Hủy";
    public const string AwaitingPayment = "Chờ thanh toán";
}

public static class PayMethod
{
    public const string Cash = "Tiền mặt";
    public const string VnPay = "VNPay";
}

public static class PayStatus
{
    public const string Unpaid = "Chưa thanh toán";
    public const string Paid = "Đã thanh toán";
}

public static class PaymentTxnStatus
{
    public const string Pending = "Chờ xử lý";
    public const string Success = "Thành công";
    public const string Failed = "Thất bại";
    public const string NeedRefund = "Cần hoàn";
}
