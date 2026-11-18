using FoodOrder.Data;
using FoodOrder.Helpers;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public class IngredientService : IIngredientService
{
    private readonly FoodOrderContext _db;
    public IngredientService(FoodOrderContext db) => _db = db;

    private static IngredientViewModel ToVm(Ingredient i) => new()
    {
        IngredientId = i.IngredientId,
        Name = i.IngredientName,
        Unit = i.Unit,
        StockQty = i.StockQty,
        MinQty = i.MinQty,
        RowVersionBase64 = Convert.ToBase64String(i.RowVersion)
    };

    public async Task<List<IngredientViewModel>> GetAllAsync(bool onlyLow = false)
    {
        var q = _db.Ingredients.AsNoTracking().AsQueryable();
        if (onlyLow) q = q.Where(i => i.StockQty < i.MinQty);
        return (await q.OrderBy(i => i.IngredientName).ToListAsync()).Select(ToVm).ToList();
    }

    public async Task<IngredientViewModel?> GetByIdAsync(int id)
    {
        var i = await _db.Ingredients.AsNoTracking().FirstOrDefaultAsync(x => x.IngredientId == id);
        return i == null ? null : ToVm(i);
    }

    public async Task<(bool, string?)> SaveAsync(IngredientViewModel vm)
    {
        var name = vm.Name.Trim();
        if (await _db.Ingredients.AnyAsync(x => x.IngredientName == name && x.IngredientId != vm.IngredientId))
            return (false, "Tên nguyên liệu đã tồn tại.");

        Ingredient e;
        if (vm.IngredientId == 0)
        {
            e = new Ingredient();
            _db.Ingredients.Add(e);
        }
        else
        {
            e = await _db.Ingredients.FirstOrDefaultAsync(x => x.IngredientId == vm.IngredientId)
                ?? throw new InvalidOperationException("Không tìm thấy nguyên liệu");
            if (!RowVersionHelper.TryParse(vm.RowVersionBase64, out var original))
                return (false, "Dữ liệu phiên bản không hợp lệ, vui lòng tải lại trang.");
            _db.Entry(e).Property(x => x.RowVersion).OriginalValue = original;
        }
        e.IngredientName = name;
        e.Unit = vm.Unit.Trim();
        e.StockQty = vm.StockQty;
        e.MinQty = vm.MinQty;

        try { await _db.SaveChangesAsync(); return (true, null); }
        catch (DbUpdateConcurrencyException) { return (false, "Nguyên liệu vừa được người khác cập nhật, vui lòng tải lại."); }
    }

    // Nhập thêm hàng: cộng vào tồn hiện tại, có kiểm tra RowVersion
    public async Task<(bool, string?)> AddStockAsync(int id, decimal qty, string rowVersionBase64)
    {
        if (qty <= 0) return (false, "Số lượng nhập phải lớn hơn 0.");
        var e = await _db.Ingredients.FirstOrDefaultAsync(x => x.IngredientId == id);
        if (e == null) return (false, "Không tìm thấy nguyên liệu.");
        if (!RowVersionHelper.TryParse(rowVersionBase64, out var original))
            return (false, "Dữ liệu phiên bản không hợp lệ, vui lòng tải lại trang.");
        _db.Entry(e).Property(x => x.RowVersion).OriginalValue = original;
        e.StockQty += qty;
        try { await _db.SaveChangesAsync(); return (true, null); }
        catch (DbUpdateConcurrencyException) { return (false, "Tồn kho vừa thay đổi, vui lòng tải lại rồi nhập lại."); }
    }

    public Task<List<RecipeLineViewModel>> GetRecipeAsync(int productId)
        => _db.ProductIngredients.AsNoTracking().Where(r => r.ProductId == productId)
              .Select(r => new RecipeLineViewModel
              {
                  IngredientId = r.IngredientId,
                  IngredientName = r.Ingredient.IngredientName,
                  Unit = r.Ingredient.Unit,
                  QtyPerServing = r.QtyPerServing
              }).OrderBy(r => r.IngredientName).ToListAsync();

    public async Task<(bool, string?)> SetRecipeLineAsync(int productId, int ingredientId, decimal qty)
    {
        if (qty <= 0) return (false, "Định mức phải lớn hơn 0.");
        var line = await _db.ProductIngredients.FindAsync(productId, ingredientId); // khóa chính kép
        if (line == null)
            _db.ProductIngredients.Add(new ProductIngredient { ProductId = productId, IngredientId = ingredientId, QtyPerServing = qty });
        else line.QtyPerServing = qty;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task RemoveRecipeLineAsync(int productId, int ingredientId)
    {
        var line = await _db.ProductIngredients.FindAsync(productId, ingredientId);
        if (line == null) return;
        _db.ProductIngredients.Remove(line);
        await _db.SaveChangesAsync();
    }
}
