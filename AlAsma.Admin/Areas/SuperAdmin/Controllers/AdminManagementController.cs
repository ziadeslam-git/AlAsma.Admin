using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.Interfaces;

namespace AlAsma.Admin.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminManagementController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthorService _authorService;

        public AdminManagementController(IUnitOfWork unitOfWork, IAuthorService authorService)
        {
            _unitOfWork = unitOfWork;
            _authorService = authorService;
        }

        // GET: SuperAdmin/AdminManagement
        public async Task<IActionResult> Index()
        {
            var allAuthors = await _unitOfWork.Authors.GetAllAsync();
            var admins = allAuthors
                .Where(a => a.Role == "Admin")
                .Select(a => new AuthorListDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Code = a.Code,
                    ContractStart = a.ContractStart,
                    ContractEnd = a.ContractEnd,
                    BasicFees = a.BasicFees,
                    ContractStatus = a.ContractStatus,
                    DaysRemaining = a.DaysRemaining
                }).ToList();
            return View(admins);
        }

        // GET: SuperAdmin/AdminManagement/Create
        public IActionResult Create()
        {
            return View(new AuthorCreateDto());
        }

        // POST: SuperAdmin/AdminManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuthorCreateDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (!await _authorService.IsCodeUniqueAsync(dto.Code))
            {
                ModelState.AddModelError("Code", "هذا الكود مستخدم بالفعل");
                return View(dto);
            }

            // Create admin: same as author but Role = "Admin" hardcoded
            var admin = new AlAsma.Admin.Models.Author
            {
                Name = dto.Name,
                Code = dto.Code.ToUpper(),
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Admin", // ALWAYS hardcoded — never from form
                ContractStart = dto.ContractStart,
                ContractEnd = dto.ContractEnd,
                BasicFees = dto.BasicFees,
                IsDeleted = false
            };

            await _unitOfWork.Authors.AddAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "تم إنشاء Admin جديد بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // GET: SuperAdmin/AdminManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(id);
            if (author == null || author.Role != "Admin") return NotFound();

            var dto = new AlAsma.Admin.DTOs.Author.AuthorEditDto
            {
                Id = author.Id,
                Name = author.Name,
                ContractStart = author.ContractStart,
                ContractEnd = author.ContractEnd,
                BasicFees = author.BasicFees
            };
            ViewBag.Code = author.Code;
            return View(dto);
        }

        // POST: SuperAdmin/AdminManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AlAsma.Admin.DTOs.Author.AuthorEditDto dto)
        {
            if (!ModelState.IsValid)
            {
                var a = await _unitOfWork.Authors.GetByIdAsync(dto.Id);
                ViewBag.Code = a?.Code;
                return View(dto);
            }

            var author = await _unitOfWork.Authors.GetByIdAsync(dto.Id);
            if (author == null || author.Role != "Admin") return NotFound();

            author.Name = dto.Name;
            author.ContractStart = dto.ContractStart;
            author.ContractEnd = dto.ContractEnd;
            author.BasicFees = dto.BasicFees;

            if (!string.IsNullOrWhiteSpace(dto.Password))
                author.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            _unitOfWork.Authors.Update(author);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "تم تعديل بيانات المدير بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // GET: SuperAdmin/AdminManagement/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(id);
            if (author == null || author.Role != "Admin") return NotFound();

            var dto = new AuthorListDto
            {
                Id = author.Id,
                Name = author.Name,
                Code = author.Code,
                BasicFees = author.BasicFees,
                ContractStatus = author.ContractStatus
            };
            return View(dto);
        }

        // POST: SuperAdmin/AdminManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(id);
            if (author == null || author.Role != "Admin") return NotFound();

            await _authorService.SoftDeleteAuthorAsync(id);
            TempData["Success"] = "تم حذف الـ Admin بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }
}
