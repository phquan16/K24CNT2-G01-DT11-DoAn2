using FoodOrder.Services;
using FoodOrder.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Controllers;

public class CartController : TableContextController
{
    private readonly ICartService _cart;
    private readonly IMenuService _menu;
    private readonly IOrderService _orders;

    public CartController(
        ITableService tables, ITableSessionService sessions,
        ICartService cart, IMenuService menu, IOrderService orders)
        : base(tables, sessions)
    {
        _cart = cart;
        _menu = menu;
        _orders = orders;
    }

    public async Task<IActionResult> Index(string? table)
    {
        var (t, _) = await ResolveAsync(table);
        if (t == null) return TableMissing();

        return View(await _orders.BuildCartAsync(_cart.GetLines(), t.TableCode, t.TableName));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string? table, int productId, int quantity, int[]? optionIds, string? note)
    {
        var (t, _) = await ResolveAsync(table);
        if (t == null) return TableMissing();

        var detail = await _menu.GetDetailAsync(productId);
        if (detail == null)
        {
            TempData["Error"] = "Món không tồn tại hoặc đã ngừng bán.";
            return RedirectToAction("Index", "Menu", new { table = t.TableCode });
        }

        if (detail.IsSoldOut)
        {
            TempData["Error"] = $"{detail.ProductName} đã hết.";
            return RedirectToAction("Detail", "Menu", new { id = productId, table = t.TableCode });
        }

        if (quantity is < 1 or > 50)
        {
            TempData["Error"] = "Số lượng mỗi món từ 1 đến 50.";
            return RedirectToAction("Detail", "Menu", new { id = productId, table = t.TableCode });
        }

        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note != null && note.Length > 300)
        {
            TempData["Error"] = "Ghi chú món tối đa 300 ký tự.";
            return RedirectToAction("Detail", "Menu", new { id = productId, table = t.TableCode });
        }

        var ids = (optionIds ?? Array.Empty<int>()).Distinct().ToList();
        var validIds = detail.Groups.SelectMany(g => g.Choices).Select(c => c.ProductOptionId).ToHashSet();
        if (ids.Any(id => !validIds.Contains(id)))
        {
            TempData["Error"] = "Tùy chọn không hợp lệ cho món này.";
            return RedirectToAction("Detail", "Menu", new { id = productId, table = t.TableCode });
        }

        foreach (var group in detail.Groups)
        {
            if (group.Choices.Count(c => ids.Contains(c.ProductOptionId)) != 1)
            {
                TempData["Error"] = $"Vui lòng chọn một {group.Name}.";
                return RedirectToAction("Detail", "Menu", new { id = productId, table = t.TableCode });
            }
        }

        _cart.Add(new CartLine
        {
            ProductId = productId,
            Quantity = quantity,
            OptionIds = ids,
            Note = note
        });

        TempData["Success"] = $"Đã thêm {detail.ProductName} vào giỏ.";
        return RedirectToAction(nameof(Index), new { table = t.TableCode });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string? table, int index, int quantity)
    {
        var (t, _) = await ResolveAsync(table);
        if (t == null) return TableMissing();

        if (quantity > 50)
        {
            TempData["Error"] = "Số lượng mỗi món tối đa 50.";
            return RedirectToAction(nameof(Index), new { table = t.TableCode });
        }

        _cart.Update(index, quantity);
        return RedirectToAction(nameof(Index), new { table = t.TableCode });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string? table, int index)
    {
        var (t, _) = await ResolveAsync(table);
        if (t == null) return TableMissing();

        _cart.Remove(index);
        return RedirectToAction(nameof(Index), new { table = t.TableCode });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(string? table)
    {
        var (t, _) = await ResolveAsync(table);
        if (t == null) return TableMissing();

        _cart.Clear();
        return RedirectToAction(nameof(Index), new { table = t.TableCode });
    }
}
