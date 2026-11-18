using FoodOrder.Models;

namespace FoodOrder.ViewModels;

public class DashboardViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal RevenueInRange { get; set; }
    public int PaidOrderCount { get; set; }
    public int OpenOrderCount { get; set; }
    public int LowStockCount { get; set; }
    public List<RevenueByDayRow> RevenueByDay { get; set; } = new();
    public List<TopProductRow> TopProducts { get; set; } = new();
    public List<PeakHourRow> PeakHours { get; set; } = new();
    public List<TableUsageRow> TableUsage { get; set; } = new();
    public List<LowStockRow> LowStock { get; set; } = new();
    public List<OrderRowViewModel> RecentOrders { get; set; } = new();
}
