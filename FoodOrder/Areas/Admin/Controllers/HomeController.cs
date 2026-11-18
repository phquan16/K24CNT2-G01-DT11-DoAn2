using FoodOrder.Models;
using FoodOrder.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrder.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class HomeController : Controller
{
    private readonly IReportService _reports;

    public HomeController(IReportService reports) => _reports = reports;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        return View(await _reports.GetDashboardAsync(today.AddDays(-6), today));
    }
}
