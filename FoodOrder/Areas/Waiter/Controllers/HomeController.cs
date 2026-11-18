using System.Security.Claims;
using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Waiter.Controllers;

[Area("Waiter")]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Waiter)]
public class HomeController : Controller
{
    private readonly IWaiterService _waiter;

    public HomeController(IWaiterService waiter) => _waiter = waiter;

    public async Task<IActionResult> Index() => View(await _waiter.GetBoardAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Serve(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (ok, error) = await _waiter.MarkServedAsync(id, userId);
        if (ok) TempData["Success"] = "Đã xác nhận phục vụ món.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseTable(int id)
    {
        var (ok, error) = await _waiter.CloseTableAsync(id);
        if (ok) TempData["Success"] = "Đã đóng bàn.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index));
    }
}
