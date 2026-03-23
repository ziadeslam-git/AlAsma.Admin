using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AlAsma.Admin.Data;
using AlAsma.Admin.DTOs.Auth;
using Microsoft.EntityFrameworkCore;

namespace AlAsma.Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                if (role == "SuperAdmin") return RedirectToAction("Index", "Dashboard", new { area = "SuperAdmin" });
                if (role == "Admin") return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                if (role == "Author") return RedirectToAction("Index", "Dashboard", new { area = "Author" });
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var author = await _context.Authors
                .FirstOrDefaultAsync(a => a.Code == dto.Code && !a.IsDeleted);

            if (author == null)
            {
                ModelState.AddModelError("", "كود أو كلمة مرور غير صحيحة");
                return View(dto);
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, author.Password))
            {
                ModelState.AddModelError("", "كود أو كلمة مرور غير صحيحة");
                return View(dto);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, author.Id.ToString()),
                new Claim(ClaimTypes.Name, author.Name),
                new Claim(ClaimTypes.Role, author.Role),
                new Claim("Code", author.Code)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (author.Role == "SuperAdmin") return RedirectToAction("Index", "Dashboard", new { area = "SuperAdmin" });
            if (author.Role == "Admin") return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (author.Role == "Author") return RedirectToAction("Index", "Dashboard", new { area = "Author" });

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
