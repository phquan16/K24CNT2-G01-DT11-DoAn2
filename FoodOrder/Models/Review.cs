using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int ProductId { get; set; }

    public long TableSessionId { get; set; }

    public string CustomerName { get; set; } = null!;

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual TableSession TableSession { get; set; } = null!;
}
