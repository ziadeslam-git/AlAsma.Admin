using System;
using System.Collections.Generic;
using AlAsma.Admin.DTOs.Sale;

namespace AlAsma.Admin.DTOs.Dashboard
{
    public class AuthorDashboardDto
    {
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorCode { get; set; } = string.Empty;
        public DateTime? ContractStart { get; set; }
        public DateTime? ContractEnd { get; set; }
        public string ContractStatus { get; set; } = string.Empty;
        public int? DaysRemaining { get; set; }
        public decimal BasicFees { get; set; }
        public decimal TotalSales { get; set; }
        public decimal NetProfit { get; set; }
        public int SalesCount { get; set; }
        public List<SaleListDto> RecentSales { get; set; } = new();
    }
}
