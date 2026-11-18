using FoodOrder.Data;
using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public record OptionDto(int ProductOptionId, string OptionName, string OptionValue, decimal ExtraPrice);

public interface IProductOptionService
{
    Task<List<OptionDto>> GetAsync(int productId);
    Task<(bool ok, string? error, OptionDto? item)> AddAsync(int productId, string name, string value, decimal extra);
    Task<bool> DeleteAsync(int optionId);
}

public class ProductOptionService : IProductOptionService
{
    private readonly FoodOrderContext _db;
    public ProductOptionService(FoodOrderContext db) => _db = db;

    public Task<List<OptionDto>> GetAsync(int productId)
        => _db.ProductOptions.AsNoTracking().Where(o => o.ProductId == productId)
              .OrderBy(o => o.OptionName).ThenBy(o => o.ExtraPrice)
              .Select(o => new OptionDto(o.ProductOptionId, o.OptionName, o.OptionValue, o.ExtraPrice))
              .ToListAsync();

    public async Task<(bool, string?, OptionDto?)> AddAsync(int productId, string name, string value, decimal extra)
    {
        name = (name ?? "").Trim(); value = (value ?? "").Trim();
        if (name.Length == 0 || value.Length == 0) return (false, "Nhóm và giá trị không được trống.", null);
        if (extra < 0) return (false, "Phụ thu không được âm.", null);
        if (!await _db.Products.AnyAsync(p => p.ProductId == productId)) return (false, "Không tìm thấy món.", null);
        if (await _db.ProductOptions.AnyAsync(o => o.ProductId == productId && o.OptionName == name && o.OptionValue == value))
            return (false, "Tùy chọn này đã tồn tại.", null);

        var o = new ProductOption { ProductId = productId, OptionName = name, OptionValue = value, ExtraPrice = extra };
        _db.ProductOptions.Add(o);
        await _db.SaveChangesAsync();
        return (true, null, new OptionDto(o.ProductOptionId, o.OptionName, o.OptionValue, o.ExtraPrice));
    }

    public async Task<bool> DeleteAsync(int optionId)
    {
        var o = await _db.ProductOptions.FindAsync(optionId);
        if (o == null) return false;
        _db.ProductOptions.Remove(o);
        await _db.SaveChangesAsync();
        return true;
    }
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
}