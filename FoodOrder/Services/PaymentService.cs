using System.Data;
using FoodOrder.Data;
using FoodOrder.Helpers;
using FoodOrder.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public class CashBillItem
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ItemStatus { get; set; } = string.Empty;
    public string? SelectedOptions { get; set; }
}

public class CashBillRow
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CashBillItem> Items { get; set; } = new();
}

public class RefundRow
{
    public long PaymentId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransactionRef { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IPaymentService
{
    Task<List<CashBillRow>> GetUnpaidCashAsync();
    Task<List<RefundRow>> GetRefundsAsync();
    Task<(bool Ok, string? Error)> ConfirmCashAsync(long orderId);
    Task<(bool Ok, string? Error)> CompleteVnPayAsync(string orderCode, Guid sessionToken, int tableId, bool success, string? responseCode);
    Task<(bool Ok, string? Error)> ApplyGatewayResultAsync(string orderCode, bool success, string? responseCode);
    bool HasGateway { get; }
    string? BuildGatewayUrl(Order order, string returnUrl, string ipAddress);
}

public class PaymentService : IPaymentService
{
    private readonly FoodOrderContext _db;
    private readonly IConfiguration _config;
    private readonly ITableSessionService _sessions;

    public PaymentService(FoodOrderContext db, IConfiguration config, ITableSessionService sessions)
    {
        _db = db;
        _config = config;
        _sessions = sessions;
    }

    public bool HasGateway =>
        !string.IsNullOrWhiteSpace(_config["VnPay:TmnCode"]) &&
        !string.IsNullOrWhiteSpace(_config["VnPay:HashSecret"]);

    public async Task<List<CashBillRow>> GetUnpaidCashAsync()
    {
        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Where(o => o.PaymentMethod == PayMethod.Cash
                && o.PaymentStatus == PayStatus.Unpaid
                && o.Status != OrderStatus.Cancelled
                && o.Status != OrderStatus.AwaitingPayment)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapBill).ToList();
    }

    public async Task<List<RefundRow>> GetRefundsAsync() =>
        await _db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentTxnStatus.NeedRefund)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new RefundRow
            {
                PaymentId = p.PaymentId,
                OrderCode = p.Order.OrderCode,
                TableName = p.Order.Table.TableName,
                Amount = p.Amount,
                TransactionRef = p.TransactionRef,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

    public async Task<(bool Ok, string? Error)> ConfirmCashAsync(long orderId)
    {
        try
        {
            await SqlProc.ExecuteAsync(_db, "dbo.sp_ConfirmCashPayment", ("@OrderId", orderId));
            return (true, null);
        }
        catch (SqlException ex)
        {
            return (false, ex.Message);
        }
    }

    public Task<(bool Ok, string? Error)> CompleteVnPayAsync(
        string orderCode, Guid sessionToken, int tableId, bool success, string? responseCode) =>
        CompleteCoreAsync(orderCode, success, responseCode, sessionRestricted: true, sessionToken, tableId);

    private async Task<(bool Ok, string? Error)> CompleteCoreAsync(
        string orderCode, bool success, string? responseCode, bool sessionRestricted, Guid sessionToken, int tableId)
    {
        long? sessionId = null;
        if (sessionRestricted)
        {
            var session = await _sessions.GetValidAsync(sessionToken, tableId);
            if (session == null) return (false, "Phiên bàn đã hết hạn. Quét lại mã QR.");
            sessionId = session.TableSessionId;
        }

        var order = await _db.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode && (sessionId == null || o.TableSessionId == sessionId));
        if (order == null) return (false, "Không tìm thấy đơn của bàn này.");
        if (order.PaymentMethod != PayMethod.VnPay) return (false, "Đơn không thanh toán VNPay.");
        if (order.PaymentStatus == PayStatus.Paid) return (true, null);
        if (order.Status == OrderStatus.Cancelled) return (false, "Đơn đã hủy.");

        if (order.ExpiredAt.HasValue && order.ExpiredAt <= DateTime.Now)
        {
            await SqlProc.ExecuteAsync(_db, "dbo.sp_ExpirePendingOrders");
            return (false, "Đơn đã quá 15 phút và được hủy, tồn kho đã hoàn.");
        }

        var pending = order.Payments
            .OrderByDescending(p => p.PaymentId)
            .FirstOrDefault(p => p.Status == PaymentTxnStatus.Pending);
        if (pending == null) return (false, "Không có giao dịch đang chờ.");

        if (!success)
        {
            pending.Status = PaymentTxnStatus.Failed;
            pending.ResponseCode = string.IsNullOrWhiteSpace(responseCode) ? "99" : responseCode;
            await _db.SaveChangesAsync();
            return (false, "Thanh toán chưa thành công. Có thể thử lại trước khi đơn hết hạn.");
        }

        pending.Status = PaymentTxnStatus.Success;
        pending.ResponseCode = string.IsNullOrWhiteSpace(responseCode) ? "00" : responseCode;
        order.PaymentStatus = PayStatus.Paid;
        order.Status = OrderStatus.New;
        order.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await SqlProc.ExecuteAsync(_db, "dbo.sp_RecalcOrderStatus", ("@OrderId", order.OrderId));
        return (true, null);
    }

    public Task<(bool Ok, string? Error)> ApplyGatewayResultAsync(string orderCode, bool success, string? responseCode) =>
        CompleteCoreAsync(orderCode, success, responseCode, sessionRestricted: false, sessionToken: Guid.Empty, tableId: 0);

    public string? BuildGatewayUrl(Order order, string returnUrl, string ipAddress)
    {
        var tmnCode = _config["VnPay:TmnCode"];
        var secret = _config["VnPay:HashSecret"];
        var baseUrl = _config["VnPay:BaseUrl"];
        if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(baseUrl))
            return null;

        var amount = (long)order.TotalAmount * 100;
        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_Amount"] = amount.ToString(),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = order.OrderCode,
            ["vnp_OrderInfo"] = "Thanh toan " + order.OrderCode,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
            ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss")
        };

        var data = string.Join("&", parameters.Select(p =>
            $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        var hash = VnPayHash.HmacSha512(secret, data);
        return baseUrl + "?" + data + "&vnp_SecureHash=" + hash;
    }

    private static CashBillRow MapBill(Order order) => new()
    {
        OrderId = order.OrderId,
        OrderCode = order.OrderCode,
        TableName = order.Table.TableName,
        Area = order.Table.Area,
        Status = order.Status,
        PaymentStatus = order.PaymentStatus,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        Items = order.OrderDetails
            .Where(d => d.ItemStatus != ItemStatus.Cancelled)
            .Select(d => new CashBillItem
            {
                ProductName = d.Product.ProductName,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                ItemStatus = d.ItemStatus,
                SelectedOptions = d.SelectedOptions
            }).ToList()
    };
}
