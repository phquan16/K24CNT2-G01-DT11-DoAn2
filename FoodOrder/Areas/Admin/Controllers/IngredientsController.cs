using FoodOrder.Models;
using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class IngredientsController : Controller
{
    private readonly IIngredientService _svc;
    public IngredientsController(IIngredientService svc) => _svc = svc;

    public async Task<IActionResult> Index(bool onlyLow = false)
    { ViewBag.OnlyLow = onlyLow; return View(await _svc.GetAllAsync(onlyLow)); }

    public IActionResult Create() => View("Form", new IngredientViewModel());

    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _svc.GetByIdAsync(id);
        return vm == null ? NotFound() : View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(IngredientViewModel vm)
    {
        if (!ModelState.IsValid) return View("Form", vm);
        var (ok, error) = await _svc.SaveAsync(vm);
        if (!ok)
        {
            ModelState.AddModelError("", error!);
            if (vm.IngredientId != 0)    // lấy lại RowVersion mới để lần lưu sau không kẹt mãi
            {
                var fresh = await _svc.GetByIdAsync(vm.IngredientId);
                if (fresh != null) { vm.RowVersionBase64 = fresh.RowVersionBase64; ModelState.Remove(nameof(vm.RowVersionBase64)); }
            }
            return View("Form", vm);
        }
        TempData["Success"] = "Đã lưu nguyên liệu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStock(int id, decimal qty, string rowVersion)
    {
        var (ok, error) = await _svc.AddStockAsync(id, qty, rowVersion);
        TempData[ok ? "Success" : "Error"] = ok ? "Đã nhập thêm hàng." : error;
        return RedirectToAction(nameof(Index));
    }

    // Định mức của một món (gọi AJAX từ trang sửa món)
    [HttpGet] public async Task<IActionResult> Recipe(int productId) => Json(await _svc.GetRecipeAsync(productId));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecipeSet(int productId, int ingredientId, decimal qty)
    {
        var (ok, error) = await _svc.SetRecipeLineAsync(productId, ingredientId, qty);
        return ok ? Json(new { ok = true }) : BadRequest(new { ok = false, error });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecipeRemove(int productId, int ingredientId)
    { await _svc.RemoveRecipeLineAsync(productId, ingredientId); return Json(new { ok = true }); }
}