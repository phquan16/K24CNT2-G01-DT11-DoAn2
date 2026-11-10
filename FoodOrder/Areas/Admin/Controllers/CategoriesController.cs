using FoodOrder.Models;
using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class CategoriesController : Controller
{
    private readonly ICategoryService _service;
    public CategoriesController(ICategoryService service) => _service = service;

    public async Task<IActionResult> Index() => View(await _service.GetAllAsync());

    [HttpGet]
    public IActionResult Create() => View("Form", new CategoryViewModel());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await _service.GetByIdAsync(id);
        if (c == null) return NotFound();
        return View("Form", new CategoryViewModel
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CategoryViewModel vm)
    {
        if (!ModelState.IsValid) return View("Form", vm);

        var (ok, error) = await _service.SaveAsync(vm);
        if (!ok)
        {
            ModelState.AddModelError(nameof(vm.CategoryName), error!);
            return View("Form", vm);
        }

        TempData["Success"] = "Đã lưu danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        if (!await _service.ToggleActiveAsync(id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
}