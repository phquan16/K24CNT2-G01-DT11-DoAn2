using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface IReportService
{
    Task<DashboardViewModel> GetDashboardAsync(DateTime from, DateTime to);
}

public class ReportService : IReportService
{
    private readonly FoodOrderContext _db;

    public ReportService(FoodOrderContext db) => _db = db;

    public async Task<DashboardViewModel> GetDashboardAsync(DateTime from, DateTime to)
    {
        from = from.Date;
        to = to.Date;
        if (to < from) (from, to) = (to, from);
        if ((to - from).TotalDays > 366) from = to.AddDays(-366);

        var start = from;
        var endExclusive = to.AddDays(1);

        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= start && o.CreatedAt < endExclusive)
            .Select(o => new { o.OrderId, o.CreatedAt, o.TotalAmount, o.Status, o.PaymentStatus, o.TableId })
            .ToListAsync();

        var paidOrders = orders
            .Where(o => o.PaymentStatus == PayStatus.Paid && o.Status != OrderStatus.Cancelled)
            .ToList();
        var businessOrders = orders
            .Where(o => o.Status != OrderStatus.AwaitingPayment && o.Status != OrderStatus.Cancelled)
            .ToList();

        var details = await _db.OrderDetails.AsNoTracking()
            .Where(d => d.ItemStatus != ItemStatus.Cancelled
                && d.Order.CreatedAt >= start && d.Order.CreatedAt < endExclusive
                && d.Order.Status != OrderStatus.AwaitingPayment
                && d.Order.Status != OrderStatus.Cancelled)
            .Select(d => new { d.ProductId, d.Product.ProductName, d.Quantity, d.UnitPrice })
            .ToListAsync();

        var tables = await _db.Tables.AsNoTracking()
            .OrderBy(t => t.Area).ThenBy(t => t.TableCode)
            .ToListAsync();

        var lowStock = await _db.LowStock.AsNoTracking()
            .OrderBy(x => x.ItemType).ThenBy(x => x.ItemName)
            .ToListAsync();

        var recent = await _db.Orders.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new OrderRowViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                TableName = o.Table.TableName,
                Area = o.Table.Area,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                ItemCount = o.OrderDetails.Count
            })
            .ToListAsync();

        return new DashboardViewModel
        {
            From = from,
            To = to,
            RevenueInRange = paidOrders.Sum(o => o.TotalAmount),
            PaidOrderCount = paidOrders.Count,
            OpenOrderCount = orders.Count(o =>
                o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled),
            LowStockCount = lowStock.Count,
            RevenueByDay = paidOrders
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueByDayRow
                {
                    RevenueDate = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                }).ToList(),
            PeakHours = businessOrders
                .GroupBy(o => o.CreatedAt.Hour)
                .OrderBy(g => g.Key)
                .Select(g => new PeakHourRow
                {
                    HourOfDay = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                }).ToList(),
            TopProducts = details
                .GroupBy(d => new { d.ProductId, d.ProductName })
                .Select(g => new TopProductRow
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(r => r.TotalQty)
                .ThenBy(r => r.ProductName)
                .Take(10)
                .ToList(),
            TableUsage = tables.Select(t =>
            {
                var rows = businessOrders.Where(o => o.TableId == t.TableId).ToList();
                return new TableUsageRow
                {
                    TableId = t.TableId,
                    TableName = t.TableName,
                    Area = t.Area,
                    OrderCount = rows.Count,
                    Revenue = rows.Sum(o => o.TotalAmount)
                };
            }).ToList(),
            LowStock = lowStock,
            RecentOrders = recent
        };
    }
}
