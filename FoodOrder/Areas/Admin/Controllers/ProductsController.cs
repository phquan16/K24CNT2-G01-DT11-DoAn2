using FoodOrder.Models;
using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IProductImageService _images;
    private readonly IProductOptionService _options;

    public ProductsController(
        IProductService products,
        ICategoryService categories,
        IProductImageService images,
        IProductOptionService options)
    {
        _products = products;
        _categories = categories;
        _images = images;
        _options = options;
    }

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
        ValidateBusinessRules(vm);
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
        ViewBag.Images = await _images.GetAsync(id);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel vm)
    {
        if (id != vm.ProductId) return BadRequest();
        ValidateBusinessRules(vm);
        if (!ModelState.IsValid) { await LoadCategoriesAsync(); ViewBag.Images = await _images.GetAsync(id); return View("Form", vm); }

        if (!await _products.UpdateAsync(vm))
        {
            var fresh = await _products.GetByIdAsync(id); // lấy RowVersion mới
            if (fresh == null) return NotFound();
            await LoadCategoriesAsync();
            ViewBag.Images = await _images.GetAsync(id);
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
        TempData["Success"] = active ? "Đã hiển thị lại món." : "Đã ẩn món.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Images(int id)
    {
        ViewData["ProductId"] = id;
        return PartialView("_Images", await _images.GetAsync(id));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImages(int id, IFormFileCollection files)
    {
        var (ok, error) = await _images.UploadAsync(id, files);
        TempData[ok ? "Success" : "Error"] = ok ? "Đã tải ảnh lên." : error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainImage(int id, int imageId)
    {
        await _images.SetMainAsync(imageId);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        await _images.DeleteAsync(imageId);
        TempData["Success"] = "Đã xóa ảnh.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ----- Tùy chọn món (AJAX) -----
    [HttpGet]
    public async Task<IActionResult> OptionList(int id) => Json(await _options.GetAsync(id));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OptionAdd(int id, string optionName, string optionValue, decimal extraPrice)
    {
        var (ok, error, item) = await _options.AddAsync(id, optionName, optionValue, extraPrice);
        return ok ? Json(new { ok = true, item }) : BadRequest(new { ok = false, error });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OptionDelete(int optionId)
        => (await _options.DeleteAsync(optionId)) ? Json(new { ok = true }) : NotFound(new { ok = false });

    private void ValidateBusinessRules(ProductViewModel vm)
    {
        if (vm.SalePrice.HasValue && vm.SalePrice.Value > vm.Price)
            ModelState.AddModelError(nameof(vm.SalePrice), "Giá khuyến mãi không được lớn hơn giá bán.");
    }

    private async Task LoadCategoriesAsync()
        => ViewBag.Categories = (await _categories.GetAllAsync())
            .Where(c => c.IsActive)
            .Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString())).ToList();
}
