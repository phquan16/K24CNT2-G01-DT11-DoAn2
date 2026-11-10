using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class OrderDetail
{
    public long OrderDetailId { get; set; }

    public long OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string? SelectedOptions { get; set; }

    public string? Note { get; set; }

    public string ItemStatus { get; set; } = null!;

    public string? InternalNote { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual Product Product { get; set; } = null!;
}
