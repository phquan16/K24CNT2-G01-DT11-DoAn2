using FoodOrder.Data;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface IMenuService
{
    Task<MenuPageViewModel> GetMenuAsync(int? categoryId, string? keyword, int page, int pageSize);
}

public class MenuService : IMenuService
{
    private readonly FoodOrderContext _db;

    public MenuService(FoodOrderContext db)
    {
        _db = db;
    }

    public async Task<MenuPageViewModel> GetMenuAsync(int? categoryId, string? keyword, int page, int pageSize)
    {
        if (page < 1) page = 1;

        var query = _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(p => p.ProductName.Contains(k));
        }

        int total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new MenuItemViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Price = p.Price,
                SalePrice = p.SalePrice,
                SpiceLevel = p.SpiceLevel,
                IsSoldOut = !p.IsAvailable || (p.Stock != null && p.Stock <= 0),
                ImageUrl = p.ProductImage.Where(t => t.IsMain).Select(t => t.ImageUrl).FirstOrDefault()
            })
            .ToListAsync();

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryChipViewModel
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            })
            .ToListAsync();

        return new MenuPageViewModel
        {
            CategoryId = categoryId,
            Keyword = keyword,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Categories = categories,
            Items = items
        };
    }
}