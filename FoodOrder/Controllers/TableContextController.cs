using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Controllers;

public abstract class TableContextController : Controller
{
    private const string SessionCookie = "fo_session";
    private readonly ITableService _tables;
    private readonly ITableSessionService _sessions;

    protected TableContextController(ITableService tables, ITableSessionService sessions)
    {
        _tables = tables;
        _sessions = sessions;
    }

    protected async Task<(Table? Table, TableSession? Session)> ResolveAsync(string? tableCode)
    {
        if (string.IsNullOrWhiteSpace(tableCode)) return (null, null);

        var table = await _tables.GetByCodeAsync(tableCode);
        if (table == null || !table.IsActive || table.Status == TableStatus.Paused)
            return (null, null);

        ViewData["TableCode"] = table.TableCode;
        ViewData["TableName"] = table.TableName;

        var session = await EnsureSessionAsync(table.TableId);
        return (table, session);
    }

    protected IActionResult TableMissing()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("~/Views/Menu/TableNotFound.cshtml");
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
