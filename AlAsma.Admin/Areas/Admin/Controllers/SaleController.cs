using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        private readonly IExportService _exportService;

        public SaleController(
            ISaleService saleService,
            IAuthorService authorService,
            IUnitOfWork unitOfWork,
            IDashboardService dashboardService,
            IExportService exportService)
        {
            _saleService = saleService;
            _authorService = authorService;
            _unitOfWork = unitOfWork;
            _dashboardService = dashboardService;
            _exportService = exportService;
        }

        // GET: Admin/Sale
        public async Task<IActionResult> Index(int page = 1, string? q = null, string? field = "book")
        {
            const int pageSize = 10;
            var (sales, totalCount, totalRevenue, totalExpenses, totalQuantity) =
                await _saleService.GetAllSalesPaginatedAsync(page, pageSize, q, field);

            ViewBag.Authors = new SelectList(await _authorService.GetAllAuthorsAsync(), "Id", "Name");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.TotalQuantity = totalQuantity;
            ViewBag.SearchQuery = q ?? string.Empty;
            ViewBag.SearchField = field ?? "book";

            return View(sales);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkEditExpenses(string? q, string? field, decimal newBasicExpenses)
        {
            // Build query matching the current filter (same logic as Index)
            var query = _unitOfWork.Sales.Query().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var lq = q.Trim().ToLower();
                query = field switch
                {
                    "author" => query.Where(s => _unitOfWork.Authors.Query()
                        .Where(a => a.Id == s.AuthorId && a.Name.ToLower().Contains(lq)).Any()),
                    "code" => query.Where(s => _unitOfWork.Authors.Query()
                        .Where(a => a.Id == s.AuthorId && a.Code.ToLower().Contains(lq)).Any()),
                    "store" => query.Where(s => s.StoreLocation.ToLower().Contains(lq)),
                    _ => query.Where(s => s.BookTitle.ToLower().Contains(lq))
                };
            }

            var updatedCount = await query.ExecuteUpdateAsync(s => s
                .SetProperty(p => p.BasicExpenses, newBasicExpenses)
                .SetProperty(p => p.TotalAmount, p => (p.SalePrice * p.Quantity) - (newBasicExpenses * p.Quantity) < 0 ? 0 : (p.SalePrice * p.Quantity) - (newBasicExpenses * p.Quantity)));

            if (updatedCount == 0)
            {
                TempData["Error"] = "لا توجد سجلات مطابقة للفرز الحالي";
                return RedirectToAction("Index", new { q = q, field = field });
            }

            TempData["Success"] = $"تم تعديل المصروفات لـ {updatedCount} سجل بنجاح";
            return RedirectToAction("Index", new { q = q, field = field });
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
                const int pageSize = 10;
                var (sales, totalCount, totalRevenue, totalExpenses, totalQuantity) =
                    await _saleService.GetAllSalesPaginatedAsync(1, pageSize);

                ViewBag.Authors = new SelectList(await _authorService.GetAllAuthorsAsync(), "Id", "Name");
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalExpenses = totalExpenses;
                ViewBag.TotalQuantity = totalQuantity;
                ViewBag.SearchQuery = string.Empty;
                ViewBag.SearchField = "book";

                TempData["Error"] = "يرجى مراجعة بيانات العملية";
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

            // Delegate entirely to SaleService — TotalAmount calculated in ONE place (CalculateTotal)
            var success = await _saleService.UpdateSaleAsync(dto);
            if (!success)
            {
                TempData["Error"] = "لم يتم العثور على العملية";
                return RedirectToAction(nameof(Index));
            }

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
        public async Task<IActionResult> ByAuthor(int authorId, int salesPage = 1, int operationsPage = 1)
        {
            var dashboard = await _dashboardService.GetAuthorDashboardAsync(authorId, salesPage, operationsPage);
            if (dashboard == null) return NotFound();
            return View(dashboard);
        }

        // GET: Admin/Sale/Export
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var sales = (await _saleService.GetAllSalesAsync()).ToList();
            var html = _exportService.BuildAllSalesHtml(sales, DateTime.Now);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/msword",
                $"AlAsma-Sales-Report-{DateTime.Now:yyyyMMdd}.doc");
        }

        // GET: Admin/Sale/ExportWordByAuthor
        [HttpGet]
        public async Task<IActionResult> ExportWordByAuthor(int authorId)
        {
            var export = await _dashboardService.GetAuthorSalesExportAsync(authorId);
            if (export == null) return NotFound();

            var html = _exportService.BuildAuthorSalesHtml(export);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/msword",
                $"مبيعات-{export.AuthorName}-{DateTime.Now:yyyyMMdd}.doc");
        }

        // GET: Admin/Sale/ExportPdfByAuthor
        [HttpGet]
        public async Task<IActionResult> ExportPdfByAuthor(int authorId)
        {
            var export = await _dashboardService.GetAuthorSalesExportAsync(authorId);
            if (export == null) return NotFound();

            var html = _exportService.BuildAuthorSalesHtml(export);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html; charset=utf-8",
                $"مبيعات-{export.AuthorName}-{DateTime.Now:yyyyMMdd}-print.html");
        }

    }
}

