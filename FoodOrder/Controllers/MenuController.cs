using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Controllers;

public class MenuController : Controller
{
    private const string SessionCookie = "fo_session";
    private const int PageSize = 12;

    private readonly ITableService _tables;
    private readonly ITableSessionService _sessions;
    private readonly IMenuService _menu;

    public MenuController(ITableService tables, ITableSessionService sessions, IMenuService menu)
    {
        _tables = tables;
        _sessions = sessions;
        _menu = menu;
    }

    public async Task<IActionResult> Index(string? table, int? categoryId, string? q, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(table)) return View("TableNotFound");

        var t = await _tables.GetByCodeAsync(table);
        if (t == null || !t.IsActive || t.Status == TableStatus.Paused)
            return View("TableNotFound");

        await EnsureSessionAsync(t.TableId);

        var vm = await _menu.GetMenuAsync(categoryId, q, page, PageSize);
        vm.TableCode = t.TableCode;
        vm.TableName = t.TableName;

        return View(vm);
    }

    private async Task<TableSession> EnsureSessionAsync(int tableId)
    {
        if (Request.Cookies.TryGetValue(SessionCookie, out var raw) && Guid.TryParse(raw, out var token))
        {
            var existing = await _sessions.GetValidAsync(token, tableId);
            if (existing != null) return existing;
        }

        var created = await _sessions.StartAsync(tableId);
        Response.Cookies.Append(SessionCookie, created.SessionToken.ToString(), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = created.ExpiredAt
        });

        return created;
    }
}