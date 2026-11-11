namespace FoodOrder.ViewModels;

public class CartLine
{
    public int ProductId { get; set; }
    public List<int> OptionIds { get; set; } = new();
    public int Quantity { get; set; }
    public string? Note { get; set; }
}

public class CartDisplayLine
{
    public int Index { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SelectedOptions { get; set; }
    public string? Note { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsSoldOut { get; set; }
    public string? Error { get; set; }
}

public class CartPageViewModel
{
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<CartDisplayLine> Lines { get; set; } = new();
    public decimal Total => Lines.Where(l => l.Error == null).Sum(l => l.UnitPrice * l.Quantity);
}

public class CheckoutViewModel
{
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<CartDisplayLine> Lines { get; set; } = new();
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public string? CustomerEmail { get; set; }
    public string? Note { get; set; }
}

public class CustomerOrderItemViewModel
{
    public long OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SelectedOptions { get; set; }
    public string? Note { get; set; }
    public string ItemStatus { get; set; } = string.Empty;
    public bool CanCancel { get; set; }
    public bool CanReview { get; set; }
}

public class CustomerOrderViewModel
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public bool CanPay { get; set; }
    public bool CanCancelOrder { get; set; }
    public List<CustomerOrderItemViewModel> Items { get; set; } = new();
}
