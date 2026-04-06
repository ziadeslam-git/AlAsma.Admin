using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;

namespace AlAsma.Admin.Controllers
{
    /// <summary>
    /// Dedicated error controller for production error handling.
    /// Does NOT depend on HomeController or area routes.
    /// </summary>
    [Route("Error")]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handles unhandled exceptions via UseExceptionHandler("/Error").
        /// Route: /Error
        /// </summary>
        [Route("")]
        public IActionResult Index()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature != null)
            {
                // Log exception without leaking details to UI
                _logger.LogError(exceptionFeature.Error,
                    "Unhandled exception on path: {Path}", exceptionFeature.Path);
            }

            ViewBag.StatusCode = 500;
            ViewBag.Title = "خطأ في النظام";
            ViewBag.Message = "نأسف، حدث خطأ غير متوقع في النظام. يرجى المحاولة مرة أخرى.";

            return View("Error");
        }

        /// <summary>
        /// Handles status code pages via UseStatusCodePagesWithReExecute("/Error/{0}").
        /// Route: /Error/{statusCode}
        /// </summary>
        [Route("{statusCode:int}")]
        public IActionResult StatusCodePage(int statusCode)
        {
            ViewBag.StatusCode = statusCode;

            switch (statusCode)
            {
                case 404:
                    ViewBag.Title = "الصفحة غير موجودة";
                    ViewBag.Message = "الصفحة التي تبحث عنها غير موجودة أو تم نقلها.";
                    break;
                case 403:
                    ViewBag.Title = "غير مصرح";
                    ViewBag.Message = "ليس لديك صلاحية للوصول إلى هذه الصفحة.";
                    break;
                case 500:
                    ViewBag.Title = "خطأ في الخادم";
                    ViewBag.Message = "حدث خطأ داخلي في الخادم. يرجى المحاولة لاحقاً.";
                    break;
                default:
                    ViewBag.Title = "خطأ";
                    ViewBag.Message = "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.";
                    break;
            }

            return View("Error");
        }
    }
}
