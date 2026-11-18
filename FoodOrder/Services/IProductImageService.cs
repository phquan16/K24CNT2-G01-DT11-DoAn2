using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.Data;
using FoodOrder.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Web.Services;

public interface IProductImageService
{
    Task<List<ProductImage>> GetAsync(int productId);
    Task<(bool ok, string? error)> UploadAsync(int productId, IFormFileCollection files);
    Task SetMainAsync(int imageId);
    Task DeleteAsync(int imageId);
}

public class ProductImageService : IProductImageService
{
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 2 * 1024 * 1024;

    private readonly FoodOrderContext _db;
    private readonly IWebHostEnvironment _env;
    public ProductImageService(FoodOrderContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    public Task<List<ProductImage>> GetAsync(int productId)
        => _db.ProductImages.AsNoTracking().Where(i => i.ProductId == productId)
              .OrderByDescending(i => i.IsMain).ThenBy(i => i.ProductImageId).ToListAsync();

    public async Task<(bool ok, string? error)> UploadAsync(int productId, IFormFileCollection files)
    {
        if (files.Count == 0) return (false, "Chưa chọn ảnh.");
        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            if (!Allowed.Contains(ext)) return (false, $"'{f.FileName}': chỉ nhận JPG, PNG, WEBP.");
            if (f.Length > MaxBytes) return (false, $"'{f.FileName}' vượt quá 2 MB.");
        }

        var dir = Path.Combine(_env.WebRootPath, "images", "products", productId.ToString());
        Directory.CreateDirectory(dir);
        var hasMain = await _db.ProductImages.AnyAsync(i => i.ProductId == productId && i.IsMain);

        foreach (var f in files)
        {
            // Đặt tên mới bằng Guid: tránh trùng tên và tránh tên file độc hại
            var name = Guid.NewGuid().ToString("N") + Path.GetExtension(f.FileName).ToLowerInvariant();
            await using (var fs = File.Create(Path.Combine(dir, name)))
                await f.CopyToAsync(fs);

            _db.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                ImageUrl = $"/images/products/{productId}/{name}",
                IsMain = !hasMain          // ảnh đầu tiên tự thành ảnh chính
            });
            hasMain = true;
        }
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task SetMainAsync(int imageId)
    {
        var img = await _db.ProductImages.FindAsync(imageId);
        if (img == null) return;

        // Bỏ cờ cũ TRƯỚC rồi mới đặt cờ mới, cùng một transaction, để không vi phạm chỉ mục duy nhất
        await using var tx = await _db.Database.BeginTransactionAsync();
        await _db.ProductImages.Where(i => i.ProductId == img.ProductId && i.IsMain)
                 .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsMain, false));
        await _db.ProductImages.Where(i => i.ProductImageId == imageId)
                 .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsMain, true));
        await tx.CommitAsync();
    }

    public async Task DeleteAsync(int imageId)
    {
        var img = await _db.ProductImages.FindAsync(imageId);
        if (img == null) return;
        var wasMain = img.IsMain;
        var productId = img.ProductId;

        var path = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        _db.ProductImages.Remove(img);
        await _db.SaveChangesAsync();
        if (File.Exists(path)) File.Delete(path);

        if (wasMain)   // xóa ảnh chính thì đẩy ảnh còn lại lên làm ảnh chính
        {
            var next = await _db.ProductImages.Where(i => i.ProductId == productId)
                                .OrderBy(i => i.ProductImageId).FirstOrDefaultAsync();
            if (next != null) { next.IsMain = true; await _db.SaveChangesAsync(); }
        }
    }
}