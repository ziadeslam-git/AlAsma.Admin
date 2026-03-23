using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.DTOs.Dashboard;
using AlAsma.Admin.DTOs.Sale;
using AlAsma.Admin.Interfaces;

namespace AlAsma.Admin.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SaleController : Controller
    {
        private readonly ISaleService _saleService;
        private readonly IAuthorService _authorService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDashboardService _dashboardService;

        public SaleController(
            ISaleService saleService,
            IAuthorService authorService,
            IUnitOfWork unitOfWork,
            IDashboardService dashboardService)
        {
            _saleService = saleService;
            _authorService = authorService;
            _unitOfWork = unitOfWork;
            _dashboardService = dashboardService;
        }

        // GET: Admin/Sale
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var (sales, totalCount) = await _saleService.GetAllSalesPaginatedAsync(page, pageSize);

            ViewBag.Authors = new SelectList(
                await _authorService.GetAllAuthorsAsync(), "Id", "Name"
            );
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View(sales);
        }

        // GET: Admin/Sale/Create
        public async Task<IActionResult> Create()
        {
            var authors = await _authorService.GetAllAuthorsAsync();
            ViewBag.Authors = new SelectList(authors, "Id", "Name");
            return View(new SaleCreateDto { SaleDate = DateTime.UtcNow });
        }

        // POST: Admin/Sale/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaleCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Authors = new SelectList(
                    await _authorService.GetAllAuthorsAsync(), "Id", "Name");
                var sales = await _saleService.GetAllSalesAsync();
                return View("Index", sales);
            }

            var success = await _saleService.CreateSaleAsync(dto);
            if (!success)
            {
                TempData["Error"] = "حدث خطأ — تحقق من بيانات المؤلف";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "تمت إضافة عملية البيع بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Sale/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SaleCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction(nameof(Index));
            }

            var sale = await _unitOfWork.Sales.GetByIdAsync(dto.Id);
            if (sale == null) return NotFound();

            sale.BookTitle = dto.BookTitle;
            sale.AuthorId = dto.AuthorId;
            sale.StoreLocation = dto.StoreLocation;
            sale.SalePrice = dto.SalePrice;
            sale.Quantity = dto.Quantity;
            sale.BasicExpenses = dto.BasicExpenses;
            // Recalculate TotalAmount server-side — NEVER from frontend
            sale.TotalAmount = Math.Max(0, (dto.SalePrice * dto.Quantity) - (dto.BasicExpenses * dto.Quantity));

            _unitOfWork.Sales.Update(sale);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "تم تعديل العملية بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Sale/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var sale = await _saleService.GetSaleByIdAsync(id);
            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        // POST: Admin/Sale/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _saleService.DeleteSaleAsync(id);
            if (!success)
            {
                return NotFound();
            }

            TempData["Success"] = "تم حذف عملية البيع";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Sale/ByAuthor/5
        [HttpGet]
        public async Task<IActionResult> ByAuthor(int authorId)
        {
            var dashboard = await _dashboardService.GetAuthorDashboardAsync(authorId);
            if (dashboard == null) return NotFound();
            return View(dashboard);
        }

        // GET: Admin/Sale/Export
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var sales = (await _saleService.GetAllSalesAsync()).ToList();

            var rows = string.Join("", sales.Select(s => $@"
<tr>
<td>{s.BookTitle}</td>
<td>{s.AuthorName}</td>
<td>{s.SalePrice:N2}</td>
<td>{s.BasicExpenses:N2}</td>
<td><strong>{s.TotalAmount:N2}</strong></td>
<td>{s.Quantity}</td>
<td>{s.StoreLocation}</td>
<td dir='ltr'>{s.SaleDate:yyyy/MM/dd HH:mm}</td>
</tr>"));

            var html = $@"<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='utf-8'>
<style>
    body {{ font-family: 'Arial', sans-serif; direction: rtl; margin: 40px; }}
    h1 {{ text-align: center; color: #064e3b; font-size: 22px; border-bottom: 3px solid #064e3b; padding-bottom: 10px; }}
    .date {{ text-align: center; color: #666; margin-bottom: 20px; }}
    table {{ width: 100%; border-collapse: collapse; font-size: 13px; }}
    th {{ background-color: #064e3b; color: white; padding: 10px 8px; text-align: center; }}
    td {{ border: 1px solid #ddd; padding: 8px; text-align: center; }}
    tr:nth-child(even) {{ background-color: #f8f9fa; }}
    .total {{ font-weight: bold; background-color: #e8f5e9 !important; }}
</style>
</head>
<body>
<h1>الأسمى للنشر والتوزيع — تقرير المبيعات</h1>
<p class='date'>تاريخ التقرير: {DateTime.Now:yyyy/MM/dd}</p>
<table>
<thead>
<tr><th>الكتاب</th><th>الكاتب</th><th>السعر</th><th>المصروفات</th><th>المجموع</th><th>الكمية</th><th>المنفذ</th><th>التاريخ</th></tr>
</thead>
<tbody>
{rows}
<tr class='total'>
<td colspan='5'>الإجمالي</td>
<td><strong>{sales.Sum(s => s.TotalAmount):N2} ج.م</strong></td>
<td><strong>{sales.Sum(s => s.Quantity)}</strong></td>
<td colspan='2'></td>
</tr>
</tbody>
</table>
</body>
</html>";

            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/msword",
                $"AlAsma-Sales-Report-{DateTime.Now:yyyyMMdd}.doc");
        }

        // GET: Admin/Sale/ExportWordByAuthor
        [HttpGet]
        public async Task<IActionResult> ExportWordByAuthor(int authorId)
        {
            var dashboard = await _dashboardService.GetAuthorDashboardAsync(authorId);
            if (dashboard == null) return NotFound();

            var html = BuildAuthorExportHtml(dashboard);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/msword",
                $"مبيعات-{dashboard.AuthorName}-{DateTime.Now:yyyyMMdd}.doc");
        }

        // GET: Admin/Sale/ExportPdfByAuthor
        [HttpGet]
        public async Task<IActionResult> ExportPdfByAuthor(int authorId)
        {
            var dashboard = await _dashboardService.GetAuthorDashboardAsync(authorId);
            if (dashboard == null) return NotFound();

            var html = BuildAuthorExportHtml(dashboard);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html; charset=utf-8",
                $"مبيعات-{dashboard.AuthorName}-{DateTime.Now:yyyyMMdd}-print.html");
        }

        private static string BuildAuthorExportHtml(AuthorDashboardDto d)
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
