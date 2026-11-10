using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace FoodOrder.Services;

public interface ITableService
{
    Task<List<Table>> GetAllAsync();
    Task<Table?> GetByIdAsync(int id);
    Task<Table?> GetByCodeAsync(string code);
    Task<(bool Ok, string? Error)> SaveAsync(TableViewModel vm);
    Task<bool> ToggleActiveAsync(int id);
    byte[] BuildQrPng(string tableCode, string baseUrl);
}

public class TableService : ITableService
{
    private readonly FoodOrderContext _db;
    public TableService(FoodOrderContext db) => _db = db;

    public Task<List<Table>> GetAllAsync() =>
        _db.Tables.AsNoTracking().OrderBy(t => t.Area).ThenBy(t => t.TableCode).ToListAsync();

    public async Task<Table?> GetByIdAsync(int id) => await _db.Tables.FindAsync(id);

    public async Task<Table?> GetByCodeAsync(string code)
    {
        var c = code.Trim().ToUpperInvariant();
        return await _db.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.TableCode == c);
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(TableViewModel vm)
    {
        var code = vm.TableCode.Trim().ToUpperInvariant();
        bool used = await _db.Tables.AnyAsync(t => t.TableCode == code && t.TableId != vm.TableId);
        if (used) return (false, "Mã bàn đã tồn tại.");

        Table entity;
        if (vm.TableId == 0)
        {
            entity = new Table { Status = TableStatus.Empty };
            _db.Tables.Add(entity);
        }
        else
        {
            var found = await _db.Tables.FindAsync(vm.TableId);
            if (found == null) return (false, "Không tìm thấy bàn.");
            entity = found;
        }

        entity.TableCode = code;
        entity.TableName = vm.TableName.Trim();
        entity.Area = string.IsNullOrWhiteSpace(vm.Area) ? null : vm.Area.Trim();
        entity.IsActive = vm.IsActive;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var t = await _db.Tables.FindAsync(id);
        if (t == null) return false;
        t.IsActive = !t.IsActive;
        await _db.SaveChangesAsync();
        return true;
    }

    public byte[] BuildQrPng(string tableCode, string baseUrl)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Menu?table={Uri.EscapeDataString(tableCode)}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(20); // 20 pixel mỗi ô, sắc nét để in
    }
}