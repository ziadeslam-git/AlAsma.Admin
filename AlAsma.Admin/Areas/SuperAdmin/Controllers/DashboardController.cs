using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlAsma.Admin.Interfaces;

namespace AlAsma.Admin.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            // SuperAdmin uses the same Admin view — no duplicate view needed
            var stats = await _dashboardService.GetAdminDashboardAsync();
            return View("~/Areas/Admin/Views/Dashboard/Index.cshtml", stats);
        }
    }
}
