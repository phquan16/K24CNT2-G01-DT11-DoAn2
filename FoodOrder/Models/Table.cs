using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class Table
{
    public int TableId { get; set; }

    public string TableCode { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public string? Area { get; set; }

    public string? QrcodePath { get; set; }

    public string Status { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<TableSession> TableSessions { get; set; } = new List<TableSession>();
}
