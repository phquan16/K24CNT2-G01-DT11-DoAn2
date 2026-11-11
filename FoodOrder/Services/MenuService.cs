using FoodOrder.Data;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface IMenuService
{
    Task<MenuPageViewModel> GetMenuAsync(int? categoryId, string? keyword, int page, int pageSize);
    Task<ProductDetailViewModel?> GetDetailAsync(int productId);
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
                ImageUrl = p.ProductImages
                    .OrderByDescending(t => t.IsMain)
                    .Select(t => t.ImageUrl)
                    .FirstOrDefault()
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

    public async Task<ProductDetailViewModel?> GetDetailAsync(int productId)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.ProductOptions)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive && p.Category.IsActive);

        if (product == null) return null;

        var reviewStats = await _db.Reviews.AsNoTracking()
            .Where(r => r.ProductId == productId)
            .GroupBy(r => r.ProductId)
            .Select(g => new { Count = g.Count(), Avg = g.Average(r => (double)r.Rating) })
            .FirstOrDefaultAsync();

        return new ProductDetailViewModel
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Description = product.Description,
            Composition = product.Composition,
            Price = product.Price,
            SalePrice = product.SalePrice,
            SpiceLevel = product.SpiceLevel,
            Calories = product.Calories,
            IsSoldOut = !product.IsAvailable || (product.Stock != null && product.Stock <= 0),
            ImageUrl = product.ProductImages
                .OrderByDescending(i => i.IsMain)
                .Select(i => i.ImageUrl)
                .FirstOrDefault(),
            ReviewCount = reviewStats?.Count ?? 0,
            AverageRating = reviewStats?.Avg,
            Groups = product.ProductOptions
                .GroupBy(o => o.OptionName)
                .OrderBy(g => g.Key)
                .Select(g => new OptionGroupViewModel
                {
                    Name = g.Key,
                    Choices = g.OrderBy(o => o.ExtraPrice).ThenBy(o => o.OptionValue)
                        .Select(o => new OptionChoiceViewModel
                        {
                            ProductOptionId = o.ProductOptionId,
                            Value = o.OptionValue,
                            ExtraPrice = o.ExtraPrice
                        }).ToList()
                }).ToList()
        };
    }
}