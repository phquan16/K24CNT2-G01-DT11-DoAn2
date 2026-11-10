using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class Order
{
    public long OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int TableId { get; set; }

    public long TableSessionId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string? InternalNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Table Table { get; set; } = null!;

    public virtual TableSession TableSession { get; set; } = null!;
}
