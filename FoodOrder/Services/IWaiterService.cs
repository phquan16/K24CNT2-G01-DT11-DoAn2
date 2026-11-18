using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface IWaiterService
{
    Task<WaiterBoardViewModel> GetBoardAsync();
    Task<(bool Ok, string? Error)> MarkServedAsync(long orderDetailId, string? userId);
    Task<(bool Ok, string? Error)> CloseTableAsync(int tableId);
}

public class WaiterService : IWaiterService
{
    private readonly FoodOrderContext _db;
    private readonly IKitchenService _kitchen;
    private readonly IOrderService _orders;

    public WaiterService(FoodOrderContext db, IKitchenService kitchen, IOrderService orders)
    {
        _db = db;
        _kitchen = kitchen;
        _orders = orders;
    }

    public async Task<WaiterBoardViewModel> GetBoardAsync()
    {
        var tables = await _db.Tables.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Area)
            .ThenBy(t => t.TableCode)
            .ToListAsync();

        var openByTable = await _db.Orders.AsNoTracking()
            .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.TableId)
            .Select(g => new
            {
                TableId = g.Key,
                OpenOrders = g.Count(),
                Unpaid = g.Sum(x => x.PaymentStatus == PayStatus.Unpaid ? x.TotalAmount : 0)
            })
            .ToListAsync();

        var readyByTable = await _db.OrderDetails.AsNoTracking()
            .Where(d => d.ItemStatus == ItemStatus.Done)
            .GroupBy(d => d.Order.TableId)
            .Select(g => new { TableId = g.Key, Count = g.Count() })
            .ToListAsync();

        return new WaiterBoardViewModel
        {
            Tables = tables.Select(t =>
            {
                var open = openByTable.FirstOrDefault(x => x.TableId == t.TableId);
                var ready = readyByTable.FirstOrDefault(x => x.TableId == t.TableId);
                return new TableBoardRowViewModel
                {
                    TableId = t.TableId,
                    TableCode = t.TableCode,
                    TableName = t.TableName,
                    Area = t.Area,
                    Status = t.Status,
                    IsActive = t.IsActive,
                    OpenOrders = open?.OpenOrders ?? 0,
                    UnpaidAmount = open?.Unpaid ?? 0,
                    ReadyItems = ready?.Count ?? 0
                };
            }).ToList(),
            Ready = await _kitchen.GetReadyAsync()
        };
    }

    public Task<(bool Ok, string? Error)> MarkServedAsync(long orderDetailId, string? userId) =>
        _kitchen.UpdateItemAsync(orderDetailId, ItemStatus.Served, userId, null, false);

    public Task<(bool Ok, string? Error)> CloseTableAsync(int tableId) =>
        _orders.CloseTableAsync(tableId);
}
