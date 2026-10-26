using System.Collections.Generic;
using System.Security.Claims;

namespace AlAsma.Admin.UI
{
    /// <summary>One entry of the app navigation (sidebar, rail and mobile bottom bar).</summary>
    public sealed record NavItem(
        string Label,
        string IconName,
        string? Area = null,
        string? Controller = null,
        string? Action = null,
        string? OpensSheet = null,
        bool InBottomBar = true);

    /// <summary>Builds the navigation for the signed-in role and tells which entry is active.</summary>
    public static class Navigation
    {
        public static IReadOnlyList<NavItem> For(ClaimsPrincipal user)
        {
            if (user.IsInRole("SuperAdmin"))
            {
                return new[]
                {
                    new NavItem("الرئيسية", "home", "SuperAdmin", "Dashboard", "Index"),
                    new NavItem("المؤلفون", "feather", "Admin", "Author", "Index"),
                    new NavItem("المبيعات", "receipt", "Admin", "Sale", "Index"),
                    new NavItem("العمليات", "wrench", "Admin", "Operation", "Index"),
                    new NavItem("إدارة Admins", "shield", "SuperAdmin", "AdminManagement", "Index", InBottomBar: false),
                };
            }

            if (user.IsInRole("Admin"))
            {
                return new[]
                {
                    new NavItem("الرئيسية", "home", "Admin", "Dashboard", "Index"),
                    new NavItem("المؤلفون", "feather", "Admin", "Author", "Index"),
                    new NavItem("المبيعات", "receipt", "Admin", "Sale", "Index"),
                    new NavItem("العمليات", "wrench", "Admin", "Operation", "Index"),
                };
            }

            return new[]
            {
                new NavItem("مبيعاتي", "receipt", "Author", "Dashboard", "Index"),
                new NavItem("تقريري", "download", OpensSheet: "report-sheet"),
                new NavItem("حسابي", "user", OpensSheet: "more-sheet", InBottomBar: false),
            };
        }

        public static bool IsActive(NavItem item, string? area, string? controller)
        {
            if (item.Controller == null) return false;
            return string.Equals(item.Controller, controller, System.StringComparison.OrdinalIgnoreCase)
                   && string.Equals(item.Area, area, System.StringComparison.OrdinalIgnoreCase);
        }

        public static string RoleLabel(ClaimsPrincipal user)
        {
            if (user.IsInRole("SuperAdmin")) return "مدير النظام";
            if (user.IsInRole("Admin")) return "مدير";
            return "مؤلف";
        }

        public static string Initial(ClaimsPrincipal user)
        {
            var name = user.Identity?.Name;
            return string.IsNullOrWhiteSpace(name) ? "؟" : name.Trim()[0].ToString().ToUpperInvariant();
        }
    }
}
