using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class OrderStatusHistory
{
    public long OrderStatusHistoryId { get; set; }

    public long OrderDetailId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Note { get; set; }

    public virtual OrderDetail OrderDetail { get; set; } = null!;
}
