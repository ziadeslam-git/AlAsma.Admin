using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Dashboard;

namespace AlAsma.Admin.Interfaces
{
    public interface IDashboardService
    {
        Task<AdminDashboardDto> GetAdminDashboardAsync();
        Task<AuthorDashboardDto?> GetAuthorDashboardAsync(int authorId);
        Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync();
    }
}
