using FoodOrder.Data;
using FoodOrder.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface ITableSessionService
{
    Task<TableSession?> GetValidAsync(Guid token, int tableId);
    Task<TableSession> StartAsync(int tableId);
}

public class TableSessionService : ITableSessionService
{
    private readonly FoodOrderContext _db;
    public TableSessionService(FoodOrderContext db) => _db = db;

    // Phiên hợp lệ = đúng token, đúng bàn, chưa đóng, chưa hết hạn
    public async Task<TableSession?> GetValidAsync(Guid token, int tableId) =>
        await _db.TableSessions.FirstOrDefaultAsync(s =>
            s.SessionToken == token && s.TableId == tableId &&
            !s.IsClosed && s.ExpiredAt > DateTime.Now);

    public async Task<TableSession> StartAsync(int tableId)
    {
        var session = new TableSession
        {
            TableId = tableId,
            SessionToken = Guid.NewGuid(),
            StartedAt = DateTime.Now,
            ExpiredAt = DateTime.Now.AddHours(4),
            IsClosed = false
        };
        _db.TableSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }
}