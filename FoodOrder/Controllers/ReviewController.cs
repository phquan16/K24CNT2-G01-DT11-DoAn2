using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Controllers;

public class ReviewController : TableContextController
{
    private readonly IReviewService _reviews;

    public ReviewController(ITableService tables, ITableSessionService sessions, IReviewService reviews)
        : base(tables, sessions)
    {
        _reviews = reviews;
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int productId, string? table)
    {
        var (t, session) = await ResolveAsync(table);
        if (t == null || session == null) return TableMissing();

        var vm = await _reviews.GetEligibleAsync(productId, t.TableId, session.SessionToken);
        if (vm == null)
        {
            TempData["Error"] = "Bạn chỉ có thể đánh giá món đã gọi và chưa được đánh giá trong phiên này.";
            return RedirectToAction("Index", "Orders", new { table = t.TableCode });
        }

        vm.TableCode = t.TableCode;
        vm.TableName = t.TableName;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReviewEditViewModel model, string? table)
    {
        var (t, session) = await ResolveAsync(table);
        if (t == null || session == null) return TableMissing();

        if (!ModelState.IsValid)
        {
            var eligible = await _reviews.GetEligibleAsync(model.ProductId, t.TableId, session.SessionToken);
            if (eligible == null)
            {
                TempData["Error"] = "Không thể đánh giá món này.";
                return RedirectToAction("Index", "Orders", new { table = t.TableCode });
            }

            model.ProductName = eligible.ProductName;
            model.TableCode = t.TableCode;
            model.TableName = t.TableName;
            return View(model);
        }

        var (ok, error) = await _reviews.SaveAsync(model, t.TableId, session.SessionToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error!);
            model.TableCode = t.TableCode;
            model.TableName = t.TableName;
            return View(model);
        }

        TempData["Success"] = "Cảm ơn bạn đã đánh giá món ăn.";
        return RedirectToAction("Index", "Orders", new { table = t.TableCode });
    }
}
