using FoodOrder.Data;
using FoodOrder.Models;
using FoodOrder.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public interface IReviewService
{
    Task<ReviewEditViewModel?> GetEligibleAsync(int productId, int tableId, Guid token);
    Task<(bool Ok, string? Error)> SaveAsync(ReviewEditViewModel model, int tableId, Guid token);
}

public class ReviewService : IReviewService
{
    private readonly FoodOrderContext _db;
    private readonly ITableSessionService _sessions;

    public ReviewService(FoodOrderContext db, ITableSessionService sessions)
    {
        _db = db;
        _sessions = sessions;
    }

    public async Task<ReviewEditViewModel?> GetEligibleAsync(int productId, int tableId, Guid token)
    {
        var session = await _sessions.GetValidAsync(token, tableId);
        if (session == null) return null;

        var purchased = await _db.OrderDetails.AsNoTracking()
            .AnyAsync(d => d.ProductId == productId
                && d.ItemStatus != ItemStatus.Cancelled
                && d.Order.TableSessionId == session.TableSessionId
                && d.Order.Status != OrderStatus.AwaitingPayment
                && d.Order.Status != OrderStatus.Cancelled);
        if (!purchased) return null;

        var reviewed = await _db.Reviews.AsNoTracking()
            .AnyAsync(r => r.ProductId == productId && r.TableSessionId == session.TableSessionId);
        if (reviewed) return null;

        var product = await _db.Products.AsNoTracking()
            .Where(p => p.ProductId == productId)
            .Select(p => new { p.ProductName })
            .FirstOrDefaultAsync();
        if (product == null) return null;

        return new ReviewEditViewModel
        {
            ProductId = productId,
            ProductName = product.ProductName
        };
    }

    public async Task<(bool Ok, string? Error)> SaveAsync(ReviewEditViewModel model, int tableId, Guid token)
    {
        if (model.Rating is < 1 or > 5) return (false, "Chọn số sao từ 1 đến 5.");
        if (string.IsNullOrWhiteSpace(model.CustomerName)) return (false, "Vui lòng nhập tên.");
        if (model.CustomerName.Trim().Length > 100) return (false, "Tên tối đa 100 ký tự.");
        if (!string.IsNullOrWhiteSpace(model.Comment) && model.Comment.Trim().Length > 500)
            return (false, "Nhận xét tối đa 500 ký tự.");

        var session = await _sessions.GetValidAsync(token, tableId);
        if (session == null) return (false, "Phiên bàn đã hết hạn. Quét lại mã QR.");

        var purchased = await _db.OrderDetails.AsNoTracking()
            .AnyAsync(d => d.ProductId == model.ProductId
                && d.ItemStatus != ItemStatus.Cancelled
                && d.Order.TableSessionId == session.TableSessionId
                && d.Order.Status != OrderStatus.AwaitingPayment
                && d.Order.Status != OrderStatus.Cancelled);
        if (!purchased) return (false, "Bạn chỉ có thể đánh giá món đã gọi trong phiên này.");

        var reviewed = await _db.Reviews.AnyAsync(r =>
            r.ProductId == model.ProductId && r.TableSessionId == session.TableSessionId);
        if (reviewed) return (false, "Bạn đã đánh giá món này trong phiên hiện tại.");

        _db.Reviews.Add(new Review
        {
            ProductId = model.ProductId,
            TableSessionId = session.TableSessionId,
            CustomerName = model.CustomerName.Trim(),
            Rating = (byte)model.Rating,
            Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim(),
            CreatedAt = DateTime.Now
        });

        try
        {
            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Bạn đã đánh giá món này trong phiên hiện tại.");
        }
    }
}
