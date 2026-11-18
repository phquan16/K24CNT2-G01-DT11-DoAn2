using System.Security.Claims;
using FoodOrder.Models;
using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class OrdersController : Controller
{
    private readonly IOrderAdminService _orders;
    private readonly IOrderService _orderActions;

    public OrdersController(IOrderAdminService orders, IOrderService orderActions)
    {
        _orders = orders;
        _orderActions = orderActions;
    }

    public async Task<IActionResult> Index(string? status, string? keyword, int page = 1)
    {
        ViewBag.Statuses = new[]
        {
            OrderStatus.AwaitingPayment, OrderStatus.New, OrderStatus.Cooking,
            OrderStatus.Served, OrderStatus.Completed, OrderStatus.Cancelled
        };
        return View(await _orders.GetListAsync(status, keyword, page, 20));
    }

    public async Task<IActionResult> Detail(long id)
    {
        var vm = await _orders.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id, string? note)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (ok, error) = await _orderActions.CancelOrderByStaffAsync(id, userId, note);
        if (ok) TempData["Success"] = "Đã hủy đơn.";
        else TempData["Error"] = error;

        return RedirectToAction(nameof(Detail), new { id });
    }
}
