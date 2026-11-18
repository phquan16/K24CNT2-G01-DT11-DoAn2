using FoodOrder.Models;

namespace FoodOrder.ViewModels;

public class WaiterBoardViewModel
{
    public List<TableBoardRowViewModel> Tables { get; set; } = new();
    public List<ReadyToServeRow> Ready { get; set; } = new();
}

public class TableBoardRowViewModel
{
    public int TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int OpenOrders { get; set; }
    public decimal UnpaidAmount { get; set; }
    public int ReadyItems { get; set; }
}
