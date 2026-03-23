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

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var authorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(authorIdClaim, out int authorId))
                return RedirectToAction("Login", "Account", new { area = "" });

            var dashboard = await _dashboardService.GetAuthorDashboardAsync(authorId);
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
            var dashboard = await _dashboardService.GetAuthorDashboardAsync(authorId);
            if (dashboard == null) return NotFound();

            var html = BuildExportHtml(dashboard);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/msword",
                $"مبيعات-{dashboard.AuthorName}-{DateTime.Now:yyyyMMdd}.doc");
        }

        private async Task<IActionResult> GeneratePdfExport(int authorId)
        {
            var dashboard = await _dashboardService.GetAuthorDashboardAsync(authorId);
            if (dashboard == null) return NotFound();

            var html = BuildExportHtml(dashboard);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            // Returns as HTML for browser print-to-PDF (no external library needed)
            return File(bytes, "text/html; charset=utf-8",
                $"مبيعات-{dashboard.AuthorName}-{DateTime.Now:yyyyMMdd}-print.html");
        }

        private static string BuildExportHtml(AuthorDashboardDto d)
        {
            var rows = string.Join("", d.RecentSales.Select(s => $@"
    <tr>
      <td>{s.BookTitle}</td>
      <td style='text-align:center'>{s.SalePrice:N2}</td>
      <td style='text-align:center'>{s.BasicExpenses:N2}</td>
      <td style='text-align:center;font-weight:bold'>{s.TotalAmount:N2}</td>
      <td style='text-align:center'>{s.Quantity}</td>
      <td style='text-align:center'>{s.StoreLocation}</td>
      <td style='text-align:center;direction:ltr'>{s.SaleDate:yyyy/MM/dd HH:mm}</td>
    </tr>"));

            var netClass = d.NetProfit >= 0 ? "green" : "red";

            return $@"<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='utf-8'>
<style>
  @page {{ margin: 20mm; }}
  body {{ font-family: Arial, sans-serif; direction: rtl; color: #1e293b; }}
  .header {{ text-align: center; border-bottom: 3px solid #064e3b; padding-bottom: 12px; margin-bottom: 20px; }}
  .header h1 {{ color: #064e3b; font-size: 20px; margin: 0 0 4px; }}
  .header p {{ color: #64748b; font-size: 13px; margin: 0; }}
  .info-grid {{ display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 20px; }}
  .info-card {{ border: 1px solid #e2e8f0; border-radius: 8px; padding: 10px 14px; }}
  .info-card .label {{ font-size: 11px; color: #94a3b8; margin-bottom: 4px; }}
  .info-card .value {{ font-size: 16px; font-weight: bold; color: #0f172a; }}
  .info-card .value.green {{ color: #059669; }}
  .info-card .value.red {{ color: #dc2626; }}
  table {{ width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 8px; }}
  th {{ background-color: #064e3b; color: white; padding: 9px 8px; text-align: center; font-weight: 600; }}
  td {{ border: 1px solid #e2e8f0; padding: 8px; }}
  tr:nth-child(even) {{ background: #f8fafc; }}
  .total-row {{ background: #ecfdf5 !important; font-weight: bold; }}
  .footer {{ text-align: center; margin-top: 20px; font-size: 11px; color: #94a3b8; border-top: 1px solid #e2e8f0; padding-top: 10px; }}
</style>
</head>
<body>
<div class='header'>
  <h1>الأسمى للنشر والتوزيع</h1>
  <p>تقرير مبيعات المؤلف — {d.AuthorName}</p>
  <p>تاريخ التقرير: {DateTime.Now:yyyy/MM/dd}</p>
</div>

<div class='info-grid'>
  <div class='info-card'>
    <div class='label'>إجمالي المبيعات</div>
    <div class='value'>{d.TotalSales:N2} <span style='font-size:11px;font-weight:normal'>ج.م</span></div>
  </div>
  <div class='info-card'>
    <div class='label'>المصاريف الأساسية</div>
    <div class='value red'>{d.BasicFees:N2} <span style='font-size:11px;font-weight:normal'>ج.م</span></div>
  </div>
  <div class='info-card'>
    <div class='label'>صافي الربح</div>
    <div class='value {netClass}'>{d.NetProfit:N2} <span style='font-size:11px;font-weight:normal'>ج.م</span></div>
  </div>
  <div class='info-card'>
    <div class='label'>عمليات البيع</div>
    <div class='value'>{d.SalesCount}</div>
  </div>
</div>

<table>
  <thead>
    <tr>
      <th>الكتاب</th><th>السعر</th><th>المصروفات</th>
      <th>المجموع</th><th>الكمية</th><th>المنفذ</th><th>التاريخ</th>
    </tr>
  </thead>
  <tbody>
    {rows}
    <tr class='total-row'>
      <td colspan='3' style='text-align:center'>الإجمالي</td>
      <td style='text-align:center'>{d.TotalSales:N2} ج.م</td>
      <td style='text-align:center'>{d.RecentSales.Sum(s => s.Quantity)}</td>
      <td colspan='2'></td>
    </tr>
  </tbody>
</table>

<div class='footer'>© {DateTime.Now.Year} جميع الحقوق محفوظة لدار الأسمى للنشر والتوزيع</div>
</body>
</html>";
        }
    }
}
