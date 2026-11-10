using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class TableSession
{
    public long TableSessionId { get; set; }

    public int TableId { get; set; }

    public Guid SessionToken { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime ExpiredAt { get; set; }

    public bool IsClosed { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Table Table { get; set; } = null!;
}
