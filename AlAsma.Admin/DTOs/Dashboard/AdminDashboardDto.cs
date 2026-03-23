using System.Collections.Generic;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.DTOs.Sale;

namespace AlAsma.Admin.DTOs.Dashboard
{
    public class AdminDashboardDto
    {
        public int TotalAuthors { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveContracts { get; set; }
        public int EndingSoonContracts { get; set; }
        public int ExpiredContracts { get; set; }
        public List<SaleListDto> RecentSales { get; set; } = new();
        public List<AuthorListDto> ExpiringAuthors { get; set; } = new();
    }
}
