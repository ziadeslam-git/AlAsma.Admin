using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using AlAsma.Admin.DTOs.Operation;
using AlAsma.Admin.Interfaces;

namespace AlAsma.Admin.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class OperationController : Controller
    {
        private readonly IOperationService _operationService;
        private readonly IAuthorService _authorService;
        private readonly ILogger<OperationController> _logger;
        private readonly IExportService _exportService;

        public OperationController(
            IOperationService operationService,
            IAuthorService authorService,
            ILogger<OperationController> logger,
            IExportService exportService)
        {
            _operationService = operationService;
            _authorService = authorService;
            _logger = logger;
            _exportService = exportService;
        }

        private async Task PopulateViewDataAsync(string? q, string? field, int page)
        {
            var (operations, totalCount, totalExpenses, totalGrossSales, netProfit) =
                await _operationService.GetAllOperationsPaginatedAsync(page, 10, q, field);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 10.0);
            ViewBag.TotalCount = totalCount;
            ViewBag.SearchQuery = q ?? string.Empty;
            ViewBag.SearchField = field ?? "operation";
            // TotalExpenses = Operations expenses + Sales Basic Expenses
            ViewBag.TotalExpenses = totalExpenses;
            // TotalSales = Gross Sales (SalePrice x Quantity)
            ViewBag.TotalSales = totalGrossSales;
            // NetProfit = Gross Sales - TotalExpenses
            ViewBag.NetProfit = netProfit;
            ViewBag.Authors = new SelectList(await _authorService.GetAllAuthorsAsync(), "Id", "Name");
            ViewBag.PageModel = operations;
        }

        public async Task<IActionResult> Index(string? q, string? field, int page = 1)
        {
            try
            {
                await PopulateViewDataAsync(q, field, page);
                return View((IEnumerable<OperationListDto>)ViewBag.PageModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load operations page");

                if (ex.Message.Contains("Invalid object name") || ex.Message.Contains("doesn't exist"))
                    TempData["Error"] = "صفحة العمليات غير جاهزة بعد لأن migrations الخاصة بها لم تُطبّق على قاعدة البيانات.";
                else
                    TempData["Error"] = "حدث خطأ غير متوقع، يرجى المحاولة مرة أخرى";

                ViewBag.Authors = new SelectList(Array.Empty<object>(), "Id", "Name");
                return View(Array.Empty<OperationListDto>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var operation = await _operationService.GetOperationByIdAsync(id);
                return View(operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load delete confirmation for operation {OperationId}", id);
                TempData["Error"] = "تعذر تحميل بيانات العملية المطلوبة للحذف";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OperationCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات العملية غير صالحة";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _operationService.CreateOperationAsync(model);
                TempData["Success"] = "تمت إضافة العملية بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create operation");

                if (ex.Message.Contains("Invalid object name") || ex.Message.Contains("doesn't exist"))
                    TempData["Error"] = "صفحة العمليات غير جاهزة بعد لأن migrations الخاصة بها لم تُطبّق على قاعدة البيانات.";
                else
                    TempData["Error"] = "حدث خطأ غير متوقع، يرجى المحاولة مرة أخرى";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OperationCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات التعديل غير صالحة";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _operationService.UpdateOperationAsync(model);
                TempData["Success"] = "تم تعديل العملية بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit operation");

                if (ex.Message.Contains("Invalid object name") || ex.Message.Contains("doesn't exist"))
                    TempData["Error"] = "صفحة العمليات غير جاهزة بعد لأن migrations الخاصة بها لم تُطبّق على قاعدة البيانات.";
                else
                    TempData["Error"] = "حدث خطأ غير متوقع، يرجى المحاولة مرة أخرى";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _operationService.DeleteOperationAsync(id);
                TempData["Success"] = "تم حذف العملية بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete operation {OperationId}", id);

                if (ex.Message.Contains("Invalid object name") || ex.Message.Contains("doesn't exist"))
                    TempData["Error"] = "صفحة العمليات غير جاهزة بعد لأن migrations الخاصة بها لم تُطبّق على قاعدة البيانات.";
                else
                    TempData["Error"] = "حدث خطأ غير متوقع، يرجى المحاولة مرة أخرى";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var (operations, _, _, _, _) = await _operationService.GetAllOperationsPaginatedAsync(1, 1000000, null, null);
            var html = _exportService.BuildAllOperationsHtml(operations, DateTime.Now);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/msword",
                $"AlAsma-Operations-Report-{DateTime.Now:yyyyMMdd}.doc");
        }
    }
}
