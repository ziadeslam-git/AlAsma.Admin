using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.DTOs.Dashboard;
using AlAsma.Admin.DTOs.Sale;
using System.Collections.Generic;

namespace AlAsma.Admin.Interfaces
{
    public interface IExportService
    {
        string BuildAuthorSalesHtml(AuthorSalesExportDto export);
        string BuildAllSalesHtml(IEnumerable<SaleListDto> sales, DateTime reportDate);
        string BuildAllOperationsHtml(IEnumerable<AlAsma.Admin.DTOs.Operation.OperationListDto> operations, DateTime reportDate);
    }
}
