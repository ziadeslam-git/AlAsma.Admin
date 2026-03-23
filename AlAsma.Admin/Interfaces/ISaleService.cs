using System.Collections.Generic;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Sale;

namespace AlAsma.Admin.Interfaces
{
    public interface ISaleService
    {
        Task<IEnumerable<SaleListDto>> GetAllSalesAsync();
        Task<(IEnumerable<SaleListDto> Sales, int TotalCount)> GetAllSalesPaginatedAsync(int page, int pageSize = 10);
        Task<IEnumerable<SaleListDto>> GetSalesByAuthorAsync(int authorId);
        Task<SaleListDto?> GetSaleByIdAsync(int id);
        Task<bool> CreateSaleAsync(SaleCreateDto dto);
        Task<bool> UpdateSaleAsync(SaleCreateDto dto);
        Task<bool> DeleteSaleAsync(int id);
        Task<decimal> GetTotalSalesByAuthorAsync(int authorId);
        Task<decimal> GetNetProfitByAuthorAsync(int authorId);
    }
}
