using CsvHelper;
using CsvHelper.Configuration;
using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FoodOrder.Services;

public interface IProductImportService
{
    Task<ImportPreviewViewModel> PreviewAsync(Stream csv);
    Task<int> CommitAsync(List<ImportRow> rows);   // trả về số món đã thêm
}

public class ProductImportService : IProductImportService
{
    private const int MaxRows = 2000;
    private readonly FoodOrderContext _db;
    public ProductImportService(FoodOrderContext db) => _db = db;

    public async Task<ImportPreviewViewModel> PreviewAsync(Stream csv)
    {
        var result = new ImportPreviewViewModel();
        var cats = await _db.Categories.AsNoTracking().Where(c => c.IsActive)
                            .ToDictionaryAsync(c => c.CategoryName.ToLower(), c => c.CategoryId);
        var existing = (await _db.Products.AsNoTracking()
                            .Select(p => new { p.ProductName, p.CategoryId }).ToListAsync())
                            .Select(p => (p.ProductName.ToLower(), p.CategoryId)).ToHashSet();
        var seen = new HashSet<(string, int)>();

        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = a => a.Header.Trim().ToLowerInvariant()
        };

        try
        {
            using var reader = new StreamReader(csv, System.Text.Encoding.UTF8, true);
            using var parser = new CsvReader(reader, cfg);
            await parser.ReadAsync(); parser.ReadHeader();
            var line = 1;
            while (await parser.ReadAsync())
            {
                line++;
                if (result.Rows.Count >= MaxRows) { result.FileError = $"Chỉ đọc tối đa {MaxRows} dòng."; break; }

                var row = new ImportRow
                {
                    Line = line,
                    ProductName = (parser.GetField("productname") ?? "").Trim(),
                    CategoryName = (parser.GetField("categoryname") ?? "").Trim(),
                    Description = parser.GetField("description")?.Trim()
                };

                var errors = new List<string>();
                if (row.ProductName.Length == 0 || row.ProductName.Length > 200) errors.Add("Tên món trống hoặc quá 200 ký tự");
                if (!cats.TryGetValue(row.CategoryName.ToLower(), out var cid)) errors.Add($"Không có danh mục '{row.CategoryName}'");
                else row.CategoryId = cid;

                if (!decimal.TryParse(parser.GetField("price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                    || price < 1000 || price > 10000000) errors.Add("Giá không hợp lệ (1.000 – 10.000.000)");
                else row.Price = price;

                var stockText = parser.GetField("stock")?.Trim();
                if (!string.IsNullOrEmpty(stockText))
                {
                    if (int.TryParse(stockText, out var st) && st >= 0) row.Stock = st;
                    else errors.Add("Tồn kho phải là số nguyên >= 0");
                }

                if (errors.Count > 0) { row.Status = "Lỗi"; row.Message = string.Join("; ", errors); }
                else
                {
                    var key = (row.ProductName.ToLower(), row.CategoryId);
                    if (existing.Contains(key) || !seen.Add(key)) { row.Status = "Trùng"; row.Message = "Đã có món cùng tên trong danh mục"; }
                }
                result.Rows.Add(row);
            }
        }
        catch (Exception ex) when (ex is CsvHelperException or IOException)
        {
            result.FileError = "Không đọc được file CSV. Kiểm tra định dạng và dòng tiêu đề.";
        }
        return result;
    }

    public async Task<int> CommitAsync(List<ImportRow> rows)
    {
        // Đọc lại và kiểm tra một lần nữa: không tin dữ liệu từ form ẩn do client gửi lên
        var valid = rows.Where(r => r.Status == "OK" && r.Price >= 1000 && r.Price <= 10000000
                                    && !string.IsNullOrWhiteSpace(r.ProductName)).ToList();
        var catIds = (await _db.Categories.Select(c => c.CategoryId).ToListAsync()).ToHashSet();
        var existing = (await _db.Products.Select(p => new { p.ProductName, p.CategoryId }).ToListAsync())
                        .Select(p => (p.ProductName.ToLower(), p.CategoryId)).ToHashSet();

        var added = 0;
        await using var tx = await _db.Database.BeginTransactionAsync();
        foreach (var r in valid)
        {
            if (!catIds.Contains(r.CategoryId) || !existing.Add((r.ProductName.Trim().ToLower(), r.CategoryId))) continue;
            _db.Products.Add(new Product
            {
                ProductName = r.ProductName.Trim(),
                CategoryId = r.CategoryId,
                Price = r.Price,
                Description = r.Description,
                Stock = r.Stock,
                IsAvailable = true,
                IsActive = true
            });
            added++;
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return added;
    }
}