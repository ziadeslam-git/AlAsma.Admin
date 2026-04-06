using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Dashboard;

namespace AlAsma.Admin.Interfaces
{
    public interface IDashboardService
    {
        Task<AdminDashboardDto> GetAdminDashboardAsync();
        Task<AuthorDashboardDto?> GetAuthorDashboardAsync(int authorId, int salesPage = 1, int operationsPage = 1);
        Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync();

        /// <summary>
        /// Returns full sales history for export purposes only (Word/PDF).
        /// Do NOT use in dashboard pages or normal page rendering.
        /// </summary>
        Task<AuthorSalesExportDto?> GetAuthorSalesExportAsync(int authorId);
    }
}
