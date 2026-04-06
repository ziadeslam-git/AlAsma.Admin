using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.DTOs.Dashboard;
using AlAsma.Admin.DTOs.Sale;
using AlAsma.Admin.Interfaces;

namespace AlAsma.Admin.Areas.Author.Controllers
{
    [Area("Author")]
    [Authorize(Roles = "Author")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IExportService _exportService;

        public DashboardController(IDashboardService dashboardService, IExportService exportService)
        {
            _dashboardService = dashboardService;
            _exportService = exportService;
        }

        public async Task<IActionResult> Index(int salesPage = 1, int operationsPage = 1)
        {
            var authorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(authorIdClaim, out int authorId))
                return RedirectToAction("Login", "Account", new { area = "" });

            // Dashboard only — uses lightweight DTO with paginated RecentSales
            var dashboard = await _dashboardService.GetAuthorDashboardAsync(authorId, salesPage, operationsPage);
            if (dashboard == null) return NotFound();

            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> ExportWord(int authorId)
        {
            var authorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(authorIdClaim, out int claimId) || claimId != authorId)
                return Forbid();

            return await GenerateWordExport(authorId);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(int authorId)
        {
            var authorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(authorIdClaim, out int claimId) || claimId != authorId)
                return Forbid();

            return await GeneratePdfExport(authorId);
        }

        private async Task<IActionResult> GenerateWordExport(int authorId)
        {
            // Use export DTO with full sales history — NOT dashboard DTO
            var export = await _dashboardService.GetAuthorSalesExportAsync(authorId);
            if (export == null) return NotFound();

            var html = _exportService.BuildAuthorSalesHtml(export);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/msword",
                $"مبيعات-{export.AuthorName}-{DateTime.Now:yyyyMMdd}.doc");
        }

        private async Task<IActionResult> GeneratePdfExport(int authorId)
        {
            var export = await _dashboardService.GetAuthorSalesExportAsync(authorId);
            if (export == null) return NotFound();

            var html = _exportService.BuildAuthorSalesHtml(export);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            // Returns as HTML for browser print-to-PDF (no external library needed)
            return File(bytes, "text/html; charset=utf-8",
                $"مبيعات-{export.AuthorName}-{DateTime.Now:yyyyMMdd}-print.html");
        }
    }
}
