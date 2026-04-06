using System;
using System.Collections.Generic;
using AlAsma.Admin.DTOs.Sale;

namespace AlAsma.Admin.DTOs.Dashboard
{
    /// <summary>
    /// Export-specific DTO carrying full sales history.
    /// Must ONLY be used in export actions (Word/PDF) — never in dashboard or normal page rendering.
    /// </summary>
    public class AuthorSalesExportDto
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
        public decimal OperationsExpenses { get; set; }
        public int OperationsCount { get; set; }

        /// <summary>
        /// Full sales history — not limited. For export purposes only.
        /// </summary>
        public List<SaleListDto> Sales { get; set; } = new();
        
        /// <summary>
        /// Full operations history.
        /// </summary>
        public List<AlAsma.Admin.DTOs.Operation.OperationListDto> Operations { get; set; } = new();
    }
}
