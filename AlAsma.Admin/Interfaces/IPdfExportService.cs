using System;
using System.Collections.Generic;
using AlAsma.Admin.DTOs.Dashboard;
using AlAsma.Admin.DTOs.Operation;
using AlAsma.Admin.DTOs.Sale;

namespace AlAsma.Admin.Interfaces
{
    /// <summary>
    /// Builds real PDF files (A4, Arabic RTL) for the reports.
    /// The Word reports are still produced by <see cref="IExportService"/>.
    /// </summary>
    public interface IPdfExportService
    {
        byte[] BuildAuthorSalesPdf(AuthorSalesExportDto export);
        byte[] BuildAllSalesPdf(IEnumerable<SaleListDto> sales, DateTime reportDate);
        byte[] BuildAllOperationsPdf(IEnumerable<OperationListDto> operations, DateTime reportDate);
    }
}
