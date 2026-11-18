using FoodOrder.Data;
using FoodOrder.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

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
        name = (name ?? "").Trim();
        value = (value ?? "").Trim();
        if (name.Length == 0 || value.Length == 0) return (false, "Nhóm và giá trị không được trống.", null);
        if (name.Length > 50 || value.Length > 50) return (false, "Nhóm và giá trị tối đa 50 ký tự.", null);
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
}
