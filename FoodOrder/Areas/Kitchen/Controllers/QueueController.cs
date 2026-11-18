using System.Security.Claims;
using FoodOrder.Models;
using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Kitchen.Controllers;

[Area("Kitchen")]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Kitchen)]
public class QueueController : Controller
{
    private readonly IKitchenService _kitchen;

    public QueueController(IKitchenService kitchen) => _kitchen = kitchen;

    public async Task<IActionResult> Index() => View(new KitchenBoardViewModel
    {
        Queue = await _kitchen.GetQueueAsync(),
        Ready = await _kitchen.GetReadyAsync()
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Start(long id) => UpdateAsync(id, ItemStatus.Cooking, null);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Done(long id) => UpdateAsync(id, ItemStatus.Done, null);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(long id, string? note) =>
        UpdateAsync(id, ItemStatus.Cancelled, string.IsNullOrWhiteSpace(note) ? "Bếp hủy món" : note);

    private async Task<IActionResult> UpdateAsync(long orderDetailId, string status, string? note)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (ok, error) = await _kitchen.UpdateItemAsync(orderDetailId, status, userId, note, false);
        if (ok) TempData["Success"] = $"Đã chuyển món sang trạng thái: {status}.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index));
    }
}
