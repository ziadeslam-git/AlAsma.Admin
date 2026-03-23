using System.Collections.Generic;
using AlAsma.Admin.DTOs.Author;

namespace AlAsma.Admin.DTOs.Dashboard
{
    public class SuperAdminDashboardDto
    {
        public int TotalAdmins { get; set; }
        public int TotalAuthors { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<AuthorListDto> AdminsList { get; set; } = new();
    }
}
