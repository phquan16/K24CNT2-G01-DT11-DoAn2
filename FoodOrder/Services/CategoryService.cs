using FoodOrder.Data;
using FoodOrder.Helpers;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<(bool Ok, string? Error)> SaveAsync(CategoryViewModel vm);
        Task<bool> ToggleActiveAsync(int id);
    }

public class CategoryService : ICategoryService
{
    private readonly FoodOrderContext _db;
    public CategoryService(FoodOrderContext db) => _db = db;

    public Task<List<Category>> GetAllAsync() =>
        _db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.CategoryName)
            .ToListAsync();

    public async Task<Category?> GetByIdAsync(int id) =>
        await _db.Categories.FindAsync(id);

    public async Task<(bool Ok, string? Error)> SaveAsync(CategoryViewModel vm)
    {
        var slug = SlugHelper.ToSlug(vm.CategoryName);
        if (string.IsNullOrEmpty(slug)) return (false, "Tên danh mục không hợp lệ.");

        bool slugUsed = await _db.Categories
            .AnyAsync(c => c.Slug == slug && c.CategoryId != vm.CategoryId);
        if (slugUsed) return (false, "Tên danh mục đã tồn tại.");

        Category entity;
        if (vm.CategoryId == 0)
        {
            entity = new Category();
            _db.Categories.Add(entity);
        }
        else
        {
            var found = await _db.Categories.FindAsync(vm.CategoryId);
            if (found == null) return (false, "Không tìm thấy danh mục.");
            entity = found;
        }

        entity.CategoryName = vm.CategoryName.Trim();
        entity.Slug = slug;
        entity.SortOrder = vm.SortOrder;
        entity.IsActive = vm.IsActive;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return false;
        c.IsActive = !c.IsActive;
        await _db.SaveChangesAsync();
        return true;
    }
}