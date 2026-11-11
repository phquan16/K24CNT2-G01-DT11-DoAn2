using System.Data;
using FoodOrder.Data;
using FoodOrder.Helpers;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface IOrderService
{
    Task<CartPageViewModel> BuildCartAsync(IReadOnlyList<CartLine> lines, string tableCode, string tableName);
    Task<(bool Ok, string? Error, string? OrderCode)> PlaceAsync(
        int tableId, Guid sessionToken, IReadOnlyList<CartLine> lines,
        string paymentMethod, string? email, string? note);
    Task<List<CustomerOrderViewModel>> GetSessionOrdersAsync(int tableId, Guid sessionToken);
    Task<(bool Ok, string? Error)> CancelItemAsync(long orderDetailId, int tableId, Guid sessionToken);
    Task<(bool Ok, string? Error)> CancelAwaitingPaymentAsync(long orderId, int tableId, Guid sessionToken);
    Task<(bool Ok, string? Error)> CloseTableAsync(int tableId);
}

public class OrderService : IOrderService
{
    private readonly FoodOrderContext _db;
    private readonly ITableSessionService _sessions;

    public OrderService(FoodOrderContext db, ITableSessionService sessions)
    {
        _db = db;
        _sessions = sessions;
    }

    public async Task<CartPageViewModel> BuildCartAsync(IReadOnlyList<CartLine> lines, string tableCode, string tableName)
    {
        var page = new CartPageViewModel { TableCode = tableCode, TableName = tableName };
        if (lines.Count == 0) return page;

        var ids = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.ProductOptions)
            .Where(p => ids.Contains(p.ProductId))
            .ToListAsync();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var product = products.FirstOrDefault(p => p.ProductId == line.ProductId);
            if (product == null)
            {
                page.Lines.Add(new CartDisplayLine
                {
                    Index = i,
                    ProductId = line.ProductId,
                    ProductName = "Món không còn tồn tại",
                    Quantity = line.Quantity,
                    Error = "Hãy xóa dòng này."
                });
                continue;
            }

            var priced = PriceLine(product, line);
            page.Lines.Add(new CartDisplayLine
            {
                Index = i,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                SelectedOptions = priced.Selected,
                Note = line.Note,
                Quantity = line.Quantity,
                UnitPrice = priced.Unit,
                IsSoldOut = !product.IsActive || !product.IsAvailable || (product.Stock != null && product.Stock < line.Quantity),
                Error = priced.Error
            });
        }

        return page;
    }

    public async Task<(bool Ok, string? Error, string? OrderCode)> PlaceAsync(
        int tableId, Guid sessionToken, IReadOnlyList<CartLine> lines,
        string paymentMethod, string? email, string? note)
    {
        if (lines.Count == 0) return (false, "Giỏ hàng trống.", null);
        if (paymentMethod != PayMethod.Cash && paymentMethod != PayMethod.VnPay)
            return (false, "Chọn tiền mặt hoặc VNPay.", null);
        if (!string.IsNullOrWhiteSpace(email) && email.Trim().Length > 256)
            return (false, "Email tối đa 256 ký tự.", null);
        if (!string.IsNullOrWhiteSpace(note) && note.Trim().Length > 500)
            return (false, "Ghi chú tối đa 500 ký tự.", null);

        var session = await _sessions.GetValidAsync(sessionToken, tableId);
        if (session == null) return (false, "Phiên bàn đã hết hạn. Quét lại mã QR.", null);

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var table = await _db.Tables.FirstOrDefaultAsync(t => t.TableId == tableId && t.IsActive);
            if (table == null || table.Status == TableStatus.Paused)
                return (false, "Bàn không thể gọi món.", null);

            var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await _db.Products
                .Include(p => p.ProductOptions)
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            var details = new List<OrderDetail>();
            decimal total = 0;

            foreach (var line in lines)
            {
                if (line.Quantity is < 1 or > 50)
                    return (false, "Số lượng mỗi món từ 1 đến 50.", null);

                var product = products.FirstOrDefault(p => p.ProductId == line.ProductId);
                if (product == null || !product.IsActive || !product.IsAvailable)
                    return (false, "Có món không còn bán.", null);
                if (!string.IsNullOrWhiteSpace(line.Note) && line.Note.Trim().Length > 300)
                    return (false, "Ghi chú món tối đa 300 ký tự.", null);

                var priced = PriceLine(product, line);
                if (priced.Error != null) return (false, priced.Error, null);
                if (priced.Selected != null && priced.Selected.Length > 500)
                    return (false, $"Tùy chọn của {product.ProductName} quá dài.", null);
                if (product.Stock.HasValue && product.Stock.Value < line.Quantity)
                    return (false, $"{product.ProductName} chỉ còn {product.Stock}.", null);

                total += priced.Unit * line.Quantity;
                details.Add(new OrderDetail
                {
                    ProductId = product.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = priced.Unit,
                    SelectedOptions = priced.Selected,
                    Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
                    ItemStatus = ItemStatus.New
                });
            }

            var recipe = await _db.ProductIngredients
                .Where(pi => productIds.Contains(pi.ProductId))
                .ToListAsync();
            var ingredientNeed = new Dictionary<int, decimal>();
            foreach (var line in lines)
            {
                foreach (var part in recipe.Where(r => r.ProductId == line.ProductId))
                {
                    ingredientNeed.TryGetValue(part.IngredientId, out var current);
                    ingredientNeed[part.IngredientId] = current + part.QtyPerServing * line.Quantity;
                }
            }

            if (ingredientNeed.Count > 0)
            {
                var ingredientIds = ingredientNeed.Keys.ToList();
                var ingredients = await _db.Ingredients
                    .Where(i => ingredientIds.Contains(i.IngredientId))
                    .ToListAsync();
                foreach (var (ingredientId, qty) in ingredientNeed)
                {
                    var ingredient = ingredients.First(i => i.IngredientId == ingredientId);
                    if (ingredient.StockQty < qty)
                        return (false, $"Nguyên liệu {ingredient.IngredientName} không đủ.", null);
                    ingredient.StockQty -= qty;
                }
            }

            foreach (var line in lines)
            {
                var product = products.First(p => p.ProductId == line.ProductId);
                if (product.Stock.HasValue) product.Stock -= line.Quantity;
            }

            var isVnPay = paymentMethod == PayMethod.VnPay;
            var order = new Order
            {
                OrderCode = await NextOrderCodeAsync(),
                TableId = table.TableId,
                TableSessionId = session.TableSessionId,
                TotalAmount = total,
                Status = isVnPay ? OrderStatus.AwaitingPayment : OrderStatus.New,
                PaymentMethod = paymentMethod,
                PaymentStatus = PayStatus.Unpaid,
                CustomerEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                InternalNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ExpiredAt = isVnPay ? DateTime.Now.AddMinutes(15) : null
            };
            foreach (var detail in details) order.OrderDetails.Add(detail);

            if (isVnPay)
            {
                order.Payments.Add(new Payment
                {
                    Amount = total,
                    PaymentMethod = PayMethod.VnPay,
                    TransactionRef = order.OrderCode,
                    Status = PaymentTxnStatus.Pending,
                    CreatedAt = DateTime.Now
                });
            }

            if (table.Status != TableStatus.Paused)
                table.Status = TableStatus.Busy;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, null, order.OrderCode);
        }
        catch (DbUpdateException)
        {
            return (false, "Đơn chưa được lưu vì tồn kho vừa thay đổi. Vui lòng thử lại.", null);
        }
    }

    public async Task<List<CustomerOrderViewModel>> GetSessionOrdersAsync(int tableId, Guid sessionToken)
    {
        var session = await _sessions.GetValidAsync(sessionToken, tableId);
        if (session == null) return new List<CustomerOrderViewModel>();

        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Where(o => o.TableSessionId == session.TableSessionId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var reviewed = await _db.Reviews.AsNoTracking()
            .Where(r => r.TableSessionId == session.TableSessionId)
            .Select(r => r.ProductId)
            .ToListAsync();

        return orders.Select(o => new CustomerOrderViewModel
        {
            OrderId = o.OrderId,
            OrderCode = o.OrderCode,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            ExpiredAt = o.ExpiredAt,
            CanPay = o.Status == OrderStatus.AwaitingPayment && o.PaymentStatus == PayStatus.Unpaid,
            CanCancelOrder = o.Status == OrderStatus.AwaitingPayment,
            Items = o.OrderDetails.Select(d => new CustomerOrderItemViewModel
            {
                OrderDetailId = d.OrderDetailId,
                ProductId = d.ProductId,
                ProductName = d.Product.ProductName,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                SelectedOptions = d.SelectedOptions,
                Note = d.Note,
                ItemStatus = d.ItemStatus,
                CanCancel = o.PaymentStatus != PayStatus.Paid
                    && o.Status != OrderStatus.AwaitingPayment
                    && d.ItemStatus == ItemStatus.New,
                CanReview = d.ItemStatus != ItemStatus.Cancelled
                    && o.Status != OrderStatus.AwaitingPayment
                    && o.Status != OrderStatus.Cancelled
                    && !reviewed.Contains(d.ProductId)
            }).ToList()
        }).ToList();
    }

    public async Task<(bool Ok, string? Error)> CancelItemAsync(long orderDetailId, int tableId, Guid sessionToken)
    {
        var session = await _sessions.GetValidAsync(sessionToken, tableId);
        if (session == null) return (false, "Phiên bàn đã hết hạn.");

        var detail = await _db.OrderDetails.AsNoTracking()
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.OrderDetailId == orderDetailId && d.Order.TableSessionId == session.TableSessionId);
        if (detail == null) return (false, "Không tìm thấy món.");
        if (detail.Order.PaymentStatus == PayStatus.Paid) return (false, "Đơn đã thanh toán, hãy gọi nhân viên.");
        if (detail.Order.Status == OrderStatus.AwaitingPayment) return (false, "Hãy hủy cả đơn đang chờ thanh toán.");
        if (detail.ItemStatus != ItemStatus.New) return (false, "Chỉ hủy được món chưa đưa vào bếp.");

        return await RunProcAsync(() => SqlProc.ExecuteAsync(
            _db,
            "dbo.sp_UpdateItemStatus",
            ("@OrderDetailId", orderDetailId),
            ("@NewStatus", ItemStatus.Cancelled),
            ("@ChangedBy", null),
            ("@Note", "Khách hủy món"),
            ("@ByCustomer", true)));
    }

    public async Task<(bool Ok, string? Error)> CancelAwaitingPaymentAsync(long orderId, int tableId, Guid sessionToken)
    {
        var session = await _sessions.GetValidAsync(sessionToken, tableId);
        if (session == null) return (false, "Phiên bàn đã hết hạn.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.TableSessionId == session.TableSessionId);

        if (order == null) return (false, "Không tìm thấy đơn.");
        if (order.Status != OrderStatus.AwaitingPayment) return (false, "Đơn không còn chờ thanh toán.");

        var freshItems = order.OrderDetails.Where(d => d.ItemStatus == ItemStatus.New).ToList();
        await RestoreStockAsync(freshItems);

        foreach (var item in freshItems)
        {
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderDetailId = item.OrderDetailId,
                OldStatus = ItemStatus.New,
                NewStatus = ItemStatus.Cancelled,
                ChangedAt = DateTime.Now,
                Note = "Khách hủy đơn chờ thanh toán"
            });
            item.ItemStatus = ItemStatus.Cancelled;
        }

        foreach (var payment in order.Payments.Where(p => p.Status == PaymentTxnStatus.Pending))
            payment.Status = PaymentTxnStatus.Failed;

        order.Status = OrderStatus.Cancelled;
        order.TotalAmount = 0;
        order.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CloseTableAsync(int tableId)
    {
        var blocking = await _db.Orders.AnyAsync(o =>
            o.TableId == tableId &&
            o.Status != OrderStatus.Completed &&
            o.Status != OrderStatus.Cancelled);
        if (blocking) return (false, "Bàn còn đơn chưa hoàn tất hoặc chưa thanh toán.");

        var table = await _db.Tables.FirstOrDefaultAsync(t => t.TableId == tableId);
        if (table == null) return (false, "Không tìm thấy bàn.");

        var sessions = await _db.TableSessions.Where(s => s.TableId == tableId && !s.IsClosed).ToListAsync();
        foreach (var item in sessions) item.IsClosed = true;
        if (table.Status != TableStatus.Paused) table.Status = TableStatus.Empty;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task RestoreStockAsync(List<OrderDetail> items)
    {
        if (items.Count == 0) return;
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.ProductId)).ToListAsync();
        var recipe = await _db.ProductIngredients.Where(pi => productIds.Contains(pi.ProductId)).ToListAsync();
        var ingredientIds = recipe.Select(r => r.IngredientId).Distinct().ToList();
        var ingredients = await _db.Ingredients.Where(i => ingredientIds.Contains(i.IngredientId)).ToListAsync();

        foreach (var item in items)
        {
            var product = products.First(p => p.ProductId == item.ProductId);
            if (product.Stock.HasValue) product.Stock += item.Quantity;
            foreach (var part in recipe.Where(r => r.ProductId == item.ProductId))
            {
                var ingredient = ingredients.First(i => i.IngredientId == part.IngredientId);
                ingredient.StockQty += part.QtyPerServing * item.Quantity;
            }
        }
    }

    private async Task<string> NextOrderCodeAsync()
    {
        var prefix = "FO" + DateTime.Now.ToString("yyyyMMdd") + "N";
        var existing = await _db.Orders
            .Where(o => o.OrderCode.StartsWith(prefix))
            .Select(o => o.OrderCode)
            .ToListAsync();
        var seq = 1;
        string code;
        do
        {
            code = prefix + seq.ToString("D3");
            seq++;
        } while (existing.Contains(code));
        return code;
    }

    private static (decimal Unit, string? Selected, string? Error) PriceLine(Product product, CartLine line)
    {
        var chosen = product.ProductOptions.Where(o => line.OptionIds.Contains(o.ProductOptionId)).ToList();
        if (chosen.Count != line.OptionIds.Distinct().Count())
            return (0, null, $"Tùy chọn không thuộc món {product.ProductName}.");

        foreach (var group in product.ProductOptions.GroupBy(o => o.OptionName))
        {
            if (chosen.Count(c => c.OptionName == group.Key) != 1)
                return (0, null, $"Chọn một {group.Key} cho {product.ProductName}.");
        }

        var selected = chosen.Count == 0
            ? null
            : string.Join("; ", chosen.OrderBy(c => c.OptionName).Select(c => $"{c.OptionName}: {c.OptionValue}"));
        var unit = (product.SalePrice ?? product.Price) + chosen.Sum(c => c.ExtraPrice);
        return (unit, selected, null);
    }

    private static async Task<(bool Ok, string? Error)> RunProcAsync(Func<Task> action)
    {
        try
        {
            await action();
            return (true, null);
        }
        catch (SqlException ex)
        {
            return (false, ex.Message);
        }
    }
}
