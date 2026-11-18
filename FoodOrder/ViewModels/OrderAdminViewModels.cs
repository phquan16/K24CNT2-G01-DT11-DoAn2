namespace FoodOrder.ViewModels;

public class OrderListViewModel
{
    public List<OrderRowViewModel> Items { get; set; } = new();
    public string? Status { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public class OrderRowViewModel
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

public class AdminOrderDetailViewModel
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public string? CustomerEmail { get; set; }
    public string? InternalNote { get; set; }
    public bool CanCancel { get; set; }
    public string? CancelBlockReason { get; set; }
    public List<OrderItemRowViewModel> Items { get; set; } = new();
    public List<PaymentRowViewModel> Payments { get; set; } = new();
}

public class OrderItemRowViewModel
{
    public long OrderDetailId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
    public string? SelectedOptions { get; set; }
    public string? Note { get; set; }
    public string ItemStatus { get; set; } = string.Empty;
    public List<OrderHistoryRowViewModel> History { get; set; } = new();
}

public class OrderHistoryRowViewModel
{
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string? Note { get; set; }
}

public class PaymentRowViewModel
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }
    public string? ResponseCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
