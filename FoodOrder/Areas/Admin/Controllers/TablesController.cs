using FoodOrder.Models;
using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class TablesController : Controller
{
    private readonly ITableService _service;
    public TablesController(ITableService service) => _service = service;

    public async Task<IActionResult> Index() => View(await _service.GetAllAsync());

    [HttpGet]
    public IActionResult Create() => View("Form", new TableViewModel());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var t = await _service.GetByIdAsync(id);
        if (t == null) return NotFound();
        return View("Form", new TableViewModel
        {
            TableId = t.TableId,
            TableCode = t.TableCode,
            TableName = t.TableName,
            Area = t.Area,
            IsActive = t.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(TableViewModel vm)
    {
        if (!ModelState.IsValid) return View("Form", vm);

        var (ok, error) = await _service.SaveAsync(vm);
        if (!ok)
        {
            ModelState.AddModelError(nameof(vm.TableCode), error!);
            return View("Form", vm);
        }

        TempData["Success"] = "Đã lưu bàn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        if (!await _service.ToggleActiveAsync(id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    // Trả về ảnh QR của bàn (có thể tải về file PNG nếu download=true)
    public async Task<IActionResult> Qr(int id, bool download = false)
    {
        var t = await _service.GetByIdAsync(id);
        if (t == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var png = _service.BuildQrPng(t.TableCode, baseUrl);

        return download
            ? File(png, "image/png", $"QR-{t.TableCode}.png")
            : File(png, "image/png");
    }

    // Trang in hàng loạt mã QR cho các bàn đang hoạt động
    public async Task<IActionResult> PrintQr()
    {
        var tables = (await _service.GetAllAsync()).Where(t => t.IsActive).ToList();
        return View(tables);
    }
}