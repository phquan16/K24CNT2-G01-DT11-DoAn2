using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface IOrderAdminService
{
    Task<OrderListViewModel> GetListAsync(string? status, string? keyword, int page, int pageSize);
    Task<AdminOrderDetailViewModel?> GetDetailAsync(long orderId);
}

public class OrderAdminService : IOrderAdminService
{
    private readonly FoodOrderContext _db;

    public OrderAdminService(FoodOrderContext db) => _db = db;

    public async Task<OrderListViewModel> GetListAsync(string? status, string? keyword, int page, int pageSize)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(o => o.OrderCode.Contains(k) || o.Table.TableName.Contains(k));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderRowViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                TableName = o.Table.TableName,
                Area = o.Table.Area,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                ItemCount = o.OrderDetails.Count
            })
            .ToListAsync();

        return new OrderListViewModel
        {
            Items = items,
            Status = status,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<AdminOrderDetailViewModel?> GetDetailAsync(long orderId)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Include(o => o.OrderDetails).ThenInclude(d => d.OrderStatusHistories)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null) return null;

        var activeItems = order.OrderDetails.Where(d => d.ItemStatus != ItemStatus.Cancelled).ToList();
        var canCancel = order.PaymentStatus != PayStatus.Paid
            && order.Status != OrderStatus.Completed
            && order.Status != OrderStatus.Cancelled;
        string? blockReason = null;

        if (!canCancel)
        {
            blockReason = order.PaymentStatus == PayStatus.Paid
                ? "Đơn đã thanh toán, không thể hủy; cần hoàn tiền theo quy trình."
                : "Đơn đã hoàn tất hoặc đã hủy.";
        }
        else if (order.Status != OrderStatus.AwaitingPayment
                 && activeItems.Any(d => d.ItemStatus != ItemStatus.New && d.ItemStatus != ItemStatus.Cooking))
        {
            canCancel = false;
            blockReason = "Đơn đã có món chế biến xong hoặc đã phục vụ; hãy thu tiền thay vì hủy.";
        }

        return new AdminOrderDetailViewModel
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            TableName = order.Table.TableName,
            Area = order.Table.Area,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            ExpiredAt = order.ExpiredAt,
            CustomerEmail = order.CustomerEmail,
            InternalNote = order.InternalNote,
            CanCancel = canCancel,
            CancelBlockReason = blockReason,
            Items = order.OrderDetails
                .OrderBy(d => d.OrderDetailId)
                .Select(d => new OrderItemRowViewModel
                {
                    OrderDetailId = d.OrderDetailId,
                    ProductName = d.Product.ProductName,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    SelectedOptions = d.SelectedOptions,
                    Note = d.Note,
                    ItemStatus = d.ItemStatus,
                    History = d.OrderStatusHistories
                        .OrderBy(h => h.ChangedAt)
                        .Select(h => new OrderHistoryRowViewModel
                        {
                            OldStatus = h.OldStatus,
                            NewStatus = h.NewStatus,
                            ChangedAt = h.ChangedAt,
                            ChangedBy = h.ChangedBy,
                            Note = h.Note
                        }).ToList()
                }).ToList(),
            Payments = order.Payments
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PaymentRowViewModel
                {
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    TransactionRef = p.TransactionRef,
                    ResponseCode = p.ResponseCode,
                    CreatedAt = p.CreatedAt
                }).ToList()
        };
    }
}
