using FoodOrder.Models;

namespace FoodOrder.ViewModels;

public class KitchenBoardViewModel
{
    public List<KitchenQueueRow> Queue { get; set; } = new();
    public List<ReadyToServeRow> Ready { get; set; } = new();
}
