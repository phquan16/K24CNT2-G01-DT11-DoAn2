using FoodOrder.Data;
using FoodOrder.Helpers;
using FoodOrder.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface IKitchenService
{
    Task<List<KitchenQueueRow>> GetQueueAsync();
    Task<List<ReadyToServeRow>> GetReadyAsync();
    Task<(bool Ok, string? Error)> UpdateItemAsync(long orderDetailId, string newStatus, string? userId, string? note, bool byCustomer);
}

public class KitchenService : IKitchenService
{
    private readonly FoodOrderContext _db;

    public KitchenService(FoodOrderContext db) => _db = db;

    public Task<List<KitchenQueueRow>> GetQueueAsync() =>
        _db.KitchenQueue.AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.OrderDetailId)
            .ToListAsync();

    public Task<List<ReadyToServeRow>> GetReadyAsync() =>
        _db.ReadyToServe.AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.OrderDetailId)
            .ToListAsync();

    public async Task<(bool Ok, string? Error)> UpdateItemAsync(
        long orderDetailId, string newStatus, string? userId, string? note, bool byCustomer)
    {
        try
        {
            await SqlProc.ExecuteAsync(
                _db,
                "dbo.sp_UpdateItemStatus",
                ("@OrderDetailId", orderDetailId),
                ("@NewStatus", newStatus),
                ("@ChangedBy", userId),
                ("@Note", note),
                ("@ByCustomer", byCustomer));
            return (true, null);
        }
        catch (SqlException ex)
        {
            return (false, ex.Message);
        }
    }
}
