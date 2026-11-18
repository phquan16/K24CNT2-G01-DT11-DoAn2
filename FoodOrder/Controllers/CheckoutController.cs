using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Controllers;

public class CheckoutController : TableContextController
{
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly IPaymentService _payments;

    public CheckoutController(
        ITableService tables, ITableSessionService sessions,
        ICartService cart, IOrderService orders, IPaymentService payments)
        : base(tables, sessions)
    {
        _cart = cart;
        _orders = orders;
        _payments = payments;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? table)
    {
        var (t, _) = await ResolveAsync(table);
        if (t == null) return TableMissing();

        var lines = _cart.GetLines();
        if (lines.Count == 0) return RedirectToAction("Index", "Cart", new { table = t.TableCode });

        ViewData["HasGateway"] = _payments.HasGateway;
        return View(await _orders.BuildCartAsync(lines, t.TableCode, t.TableName));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(string? table, string paymentMethod, string? email, string? note)
    {
        var (t, session) = await ResolveAsync(table);
        if (t == null || session == null) return TableMissing();

        if (paymentMethod != PayMethod.Cash && paymentMethod != PayMethod.VnPay)
        {
            TempData["Error"] = "Vui lòng chọn phương thức thanh toán.";
            return RedirectToAction(nameof(Index), new { table = t.TableCode });
        }

        if (paymentMethod == PayMethod.VnPay && !_payments.HasGateway)
        {
            TempData["Error"] = "VNPay chưa được cấu hình trên hệ thống. Vui lòng chọn thanh toán tiền mặt tại quầy.";
            return RedirectToAction(nameof(Index), new { table = t.TableCode });
        }

        var lines = _cart.GetLines();
        if (lines.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Cart", new { table = t.TableCode });
        }

        var (ok, error, orderCode) = await _orders.PlaceAsync(
            t.TableId, session.SessionToken, lines, paymentMethod, email, note);

        if (!ok)
        {
            TempData["Error"] = error ?? "Không gửi được đơn. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index), new { table = t.TableCode });
        }

        _cart.Clear();

        if (paymentMethod == PayMethod.VnPay && orderCode != null)
        {
            var returnUrl = Url.Action("VnPayReturn", "Orders", new { table = t.TableCode }, Request.Scheme)!;
            var url = await _payments.BuildGatewayUrlForOrderAsync(
                orderCode, returnUrl, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            if (url != null) return Redirect(url);

            TempData["Error"] = "Không tạo được liên kết VNPay. Đơn đã được tạo và sẽ tự hủy sau 15 phút.";
        }
        else
        {
            TempData["Success"] = $"Đã gửi đơn {orderCode}. Vui lòng thanh toán tại quầy khi dùng xong.";
        }

        return RedirectToAction("Index", "Orders", new { table = t.TableCode });
    }
}
