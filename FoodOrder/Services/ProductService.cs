using FoodOrder.Data;
using FoodOrder.Helpers;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public class ProductService : IProductService
{
    private readonly FoodOrderContext _db;
    public ProductService(FoodOrderContext db) => _db = db;

    public async Task<ProductListViewModel> GetListAsync(int? categoryId, string? keyword, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 15;

        var q = _db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.ProductImages).AsQueryable();

        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(p => p.ProductName.Contains(keyword.Trim())); // collation tiếng Việt không phân biệt hoa thường

        var total = await q.CountAsync();
        var items = await q.OrderBy(p => p.ProductName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProductRowViewModel
            {
                ProductId = p.ProductId,
                Name = p.ProductName,
                CategoryName = p.Category.CategoryName,
                Price = p.Price,
                SalePrice = p.SalePrice,
                Stock = p.Stock,
                IsAvailable = p.IsAvailable,
                IsActive = p.IsActive,
                MainImage = p.ProductImages.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
            }).ToListAsync();

        return new ProductListViewModel
        {
            Items = items,
            CategoryId = categoryId,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.CategoryName)
                .Select(c => new CategoryViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive
                }).ToListAsync()
        };
    }

    public async Task<ProductViewModel?> GetByIdAsync(int id)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == id);
        if (p == null) return null;
        return new ProductViewModel
        {
            ProductId = p.ProductId,
            Name = p.ProductName,
            CategoryId = p.CategoryId,
            Price = p.Price,
            SalePrice = p.SalePrice,
            Description = p.Description,
            Composition = p.Composition,
            SpiceLevel = p.SpiceLevel,
            Calories = p.Calories,
            Stock = p.Stock,
            IsAvailable = p.IsAvailable,
            IsActive = p.IsActive,
            RowVersionBase64 = Convert.ToBase64String(p.RowVersion)
        };
    }

    public async Task<int> CreateAsync(ProductViewModel vm)
    {
        var p = new Product
        {
            ProductName = vm.Name.Trim(),
            CategoryId = vm.CategoryId,
            Price = vm.Price,
            SalePrice = vm.SalePrice,
            Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
            Composition = string.IsNullOrWhiteSpace(vm.Composition) ? null : vm.Composition.Trim(),
            SpiceLevel = vm.SpiceLevel,
            Calories = vm.Calories,
            Stock = vm.Stock,
            IsAvailable = vm.IsAvailable,
            IsActive = true
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        return p.ProductId;
    }

    public async Task<bool> UpdateAsync(ProductViewModel vm)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == vm.ProductId);
        if (p == null) return false;

        // Gán RowVersion CŨ (lúc mở form) vào OriginalValue.
        // EF sẽ sinh: UPDATE ... WHERE ProductId = @id AND RowVersion = @old
        if (!RowVersionHelper.TryParse(vm.RowVersionBase64, out var original)) return false;
        _db.Entry(p).Property(x => x.RowVersion).OriginalValue = original;

        p.ProductName = vm.Name.Trim();
        p.CategoryId = vm.CategoryId;
        p.Price = vm.Price;
        p.SalePrice = vm.SalePrice;
        p.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
        p.Composition = string.IsNullOrWhiteSpace(vm.Composition) ? null : vm.Composition.Trim();
        p.SpiceLevel = vm.SpiceLevel;
        p.Calories = vm.Calories;
        p.Stock = vm.Stock;
        p.IsAvailable = vm.IsAvailable;
        p.IsActive = vm.IsActive;

        try { await _db.SaveChangesAsync(); return true; }
        catch (DbUpdateConcurrencyException) { return false; }
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return;
        p.IsActive = active;
        if (!active) p.IsAvailable = false;
        await _db.SaveChangesAsync();
    }
}
