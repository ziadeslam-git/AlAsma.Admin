using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.Interfaces;

namespace AlAsma.Admin.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AuthorController : Controller
    {
        private readonly IAuthorService _authorService;

        public AuthorController(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var (authors, totalCount) = await _authorService.GetAllAuthorsPaginatedAsync(page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            return View(authors);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new AuthorCreateDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuthorCreateDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var success = await _authorService.CreateAuthorAsync(dto);
            if (!success)
            {
                ModelState.AddModelError("Code", "الكود مستخدم مسبقاً");
                return View(dto);
            }

            TempData["Success"] = "تم إضافة المؤلف بنجاح";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var author = await _authorService.GetAuthorByIdAsync(id);
            if (author == null) return NotFound();

            var dto = new AuthorEditDto
            {
                Id = author.Id,
                Name = author.Name,
                ContractStart = author.ContractStart,
                ContractEnd = author.ContractEnd,
                BasicFees = author.BasicFees
            };

            ViewBag.Code = author.Code; // For display only
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AuthorEditDto dto)
        {
            if (!ModelState.IsValid)
            {
                var author = await _authorService.GetAuthorByIdAsync(dto.Id);
                if (author != null)
                {
                    ViewBag.Code = author.Code;
                }
                return View(dto);
            }

            var success = await _authorService.UpdateAuthorAsync(dto);
            if (!success) return NotFound();

            TempData["Success"] = "تم تعديل بيانات المؤلف";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var author = await _authorService.GetAuthorByIdAsync(id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _authorService.SoftDeleteAuthorAsync(id);
            if (!success) return NotFound();

            TempData["Success"] = "تم حذف المؤلف";
            return RedirectToAction("Index");
        }
    }
}
