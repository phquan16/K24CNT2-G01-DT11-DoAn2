using FoodOrder.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Controllers;

public class OrdersController : TableContextController
{
    private readonly IOrderService _orders;
    private readonly IPaymentService _payments;

    public OrdersController(
        ITableService tables, ITableSessionService sessions,
        IOrderService orders, IPaymentService payments)
        : base(tables, sessions)
    {
        _orders = orders;
        _payments = payments;
    }

    public async Task<IActionResult> Index(string? table)
    {
        var (t, session) = await ResolveAsync(table);
        if (t == null || session == null) return TableMissing();

        ViewData["TableCode"] = t.TableCode;
        ViewData["TableName"] = t.TableName;
        ViewData["HasGateway"] = _payments.HasGateway;
        return View(await _orders.GetSessionOrdersAsync(t.TableId, session.SessionToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelItem(string? table, long id)
    {
        var (t, session) = await ResolveAsync(table);
        if (t == null || session == null) return TableMissing();

        var (ok, error) = await _orders.CancelItemAsync(id, t.TableId, session.SessionToken);
        if (ok) TempData["Success"] = "Đã hủy món.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index), new { table = t.TableCode });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(string? table, long id)
    {
        var (t, session) = await ResolveAsync(table);
        if (t == null || session == null) return TableMissing();

        var (ok, error) = await _orders.CancelAwaitingPaymentAsync(id, t.TableId, session.SessionToken);
        if (ok) TempData["Success"] = "Đã hủy đơn chờ thanh toán.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index), new { table = t.TableCode });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(string? table, long id)
    {
        var (t, session) = await ResolveAsync(table);
        if (t == null || session == null) return TableMissing();

        var order = (await _orders.GetSessionOrdersAsync(t.TableId, session.SessionToken))
            .FirstOrDefault(o => o.OrderId == id && o.CanPay);
        if (order == null)
        {
            TempData["Error"] = "Đơn không ở trạng thái chờ thanh toán.";
            return RedirectToAction(nameof(Index), new { table = t.TableCode });
        }

        var returnUrl = Url.Action(nameof(VnPayReturn), "Orders", new { table = t.TableCode }, Request.Scheme)!;
        var url = await _payments.BuildGatewayUrlForOrderAsync(
            order.OrderCode, returnUrl, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
        if (url == null)
        {
            TempData["Error"] = "Không tạo được liên kết thanh toán VNPay.";
            return RedirectToAction(nameof(Index), new { table = t.TableCode });
        }

        return Redirect(url);
    }

    [HttpGet]
    public async Task<IActionResult> VnPayReturn(string? table)
    {
        var (t, session) = await ResolveAsync(table);
        if (t == null || session == null) return TableMissing();

        var query = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Request.Query) query[pair.Key] = pair.Value.ToString();

        var (ok, error) = await _payments.ApplyVnPayReturnAsync(query, session.SessionToken, t.TableId);
        if (ok) TempData["Success"] = "Thanh toán VNPay thành công.";
        else TempData["Error"] = error ?? "Thanh toán chưa thành công.";
        return RedirectToAction(nameof(Index), new { table = t.TableCode });
    }
}
