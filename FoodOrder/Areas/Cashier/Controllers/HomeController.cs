using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Cashier.Controllers;

[Area("Cashier")]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Cashier)]
public class HomeController : Controller
{
    private readonly IPaymentService _payments;

    public HomeController(IPaymentService payments) => _payments = payments;

    public async Task<IActionResult> Index()
    {
        var (cashTotal, cashCount, vnPayPending) = await _payments.GetTodaySummaryAsync();
        ViewData["CashTotal"] = cashTotal;
        ViewData["CashCount"] = cashCount;
        ViewData["VnPayPending"] = vnPayPending;
        ViewData["Refunds"] = await _payments.GetRefundsAsync();
        return View(await _payments.GetUnpaidCashAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCash(long id)
    {
        var (ok, error) = await _payments.ConfirmCashAsync(id);
        if (ok) TempData["Success"] = "Đã xác nhận thu tiền mặt.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index));
    }
}
