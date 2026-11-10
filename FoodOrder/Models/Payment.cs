using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long OrderId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? TransactionRef { get; set; }

    public string Status { get; set; } = null!;

    public string? ResponseCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
