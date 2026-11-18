using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Controllers;

public class MenuController : TableContextController
{
    private const int PageSize = 12;

    private readonly IMenuService _menu;

    public MenuController(ITableService tables, ITableSessionService sessions, IMenuService menu)
        : base(tables, sessions)
    {
        _menu = menu;
    }

    public async Task<IActionResult> Index(string? table, int? categoryId, string? q, int page = 1)
    {
        var (t, _) = await ResolveAsync(table);
        if (t == null) return TableMissing();

        var vm = await _menu.GetMenuAsync(categoryId, q, page, PageSize);
        vm.TableCode = t.TableCode;
        vm.TableName = t.TableName;

        return View(vm);
    }

    public async Task<IActionResult> Detail(int id, string? table)
    {
        var (t, _) = await ResolveAsync(table);
        if (t == null) return TableMissing();

        var vm = await _menu.GetDetailAsync(id);
        if (vm == null) return NotFound();

        vm.TableCode = t.TableCode;
        return View(vm);
    }
}
