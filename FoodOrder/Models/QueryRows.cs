namespace FoodOrder.Models;

public class KitchenQueueRow
{
    public long OrderDetailId { get; set; }
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SelectedOptions { get; set; }
    public string? Note { get; set; }
    public string? InternalNote { get; set; }
    public string? OrderNote { get; set; }
    public string ItemStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ReadyToServeRow
{
    public long OrderDetailId { get; set; }
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RevenueByDayRow
{
    public DateTime? RevenueDate { get; set; }
    public int? OrderCount { get; set; }
    public decimal? Revenue { get; set; }
}

public class PeakHourRow
{
    public int? HourOfDay { get; set; }
    public int? OrderCount { get; set; }
    public decimal? Revenue { get; set; }
}

public class TopProductRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? TotalQty { get; set; }
    public decimal? TotalRevenue { get; set; }
}

public class TableUsageRow
{
    public int TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? Area { get; set; }
    public int? OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class LowStockRow
{
    public string ItemType { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal? CurrentQty { get; set; }
    public decimal? ThresholdQty { get; set; }
}
