using FoodOrder.Models;
using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ProductImportController : Controller
{
    private readonly IProductImportService _svc;
    public ProductImportController(IProductImportService svc) => _svc = svc;

    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> Preview(IFormFile file)
    {
        if (file == null || file.Length == 0 || !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        { TempData["Error"] = "Vui lòng chọn file .csv (tối đa 2 MB)."; return RedirectToAction(nameof(Index)); }

        await using var s = file.OpenReadStream();
        var vm = await _svc.PreviewAsync(s);
        HttpContext.Session.SetString("ImportRows", JsonSerializer.Serialize(vm.Rows.Where(r => r.Status == "OK").ToList()));
        return View("Preview", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Commit()
    {
        var json = HttpContext.Session.GetString("ImportRows");
        if (string.IsNullOrEmpty(json)) { TempData["Error"] = "Phiên nhập đã hết hạn, vui lòng tải lại file."; return RedirectToAction(nameof(Index)); }
        var rows = JsonSerializer.Deserialize<List<ImportRow>>(json) ?? new();
        var added = await _svc.CommitAsync(rows);
        TempData["Success"] = $"Đã nhập {added} món mới.";
        return RedirectToAction("Index", "Products");
    }
}