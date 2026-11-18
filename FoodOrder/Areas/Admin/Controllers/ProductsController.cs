using FoodOrder.Models;
using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrder.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    public ProductsController(IProductService products, ICategoryService categories)
    { _products = products; _categories = categories; }

    public async Task<IActionResult> Index(int? categoryId, string? keyword, int page = 1)
        => View(await _products.GetListAsync(categoryId, keyword, page, 15));

    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View("Form", new ProductViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel vm)
    {
        if (!ModelState.IsValid) { await LoadCategoriesAsync(); return View("Form", vm); }
        var id = await _products.CreateAsync(vm);
        TempData["Success"] = "Đã thêm món. Bạn có thể thêm ảnh và tùy chọn ngay bây giờ.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _products.GetByIdAsync(id);
        if (vm == null) return NotFound();
        await LoadCategoriesAsync();
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel vm)
    {
        if (id != vm.ProductId) return BadRequest();
        if (!ModelState.IsValid) { await LoadCategoriesAsync(); return View("Form", vm); }

        if (!await _products.UpdateAsync(vm))
        {
            ModelState.AddModelError("", "Món này vừa được người khác chỉnh sửa. Dữ liệu mới nhất đã được tải lại, vui lòng kiểm tra rồi lưu lại.");
            var fresh = await _products.GetByIdAsync(id);   // lấy RowVersion mới
            if (fresh == null) return NotFound();
            await LoadCategoriesAsync();
            ModelState.Clear();
            ModelState.AddModelError("", "Món này vừa được người khác chỉnh sửa. Dữ liệu mới nhất đã được tải lại, vui lòng kiểm tra rồi lưu lại.");
            return View("Form", fresh);
        }
        TempData["Success"] = "Đã lưu thay đổi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, bool active)
    {
        await _products.SetActiveAsync(id, active);
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync()
        => ViewBag.Categories = (await _categories.GetAllAsync())
            .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())).ToList();
    [HttpGet]
    public async Task<IActionResult> Images(int id)
    => PartialView("_Images", (id, await _images.GetAsync(id)));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImages(int id, IFormFileCollection files)
    {
        var (ok, error) = await _images.UploadAsync(id, files);
        if (!ok) TempData["Error"] = error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainImage(int id, int imageId)
    { await _images.SetMainAsync(imageId); return RedirectToAction(nameof(Edit), new { id }); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    { await _images.DeleteAsync(imageId); return RedirectToAction(nameof(Edit), new { id }); }
}