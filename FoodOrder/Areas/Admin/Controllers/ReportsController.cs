using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ReportsController : Controller
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var end = (to ?? DateTime.Today).Date;
        var start = (from ?? end.AddDays(-6)).Date;
        if (start > end) (start, end) = (end, start);

        return View(await _reports.GetDashboardAsync(start, end));
    }
}
