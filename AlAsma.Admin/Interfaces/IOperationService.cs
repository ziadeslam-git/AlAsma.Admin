using System.Collections.Generic;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Operation;

namespace AlAsma.Admin.Interfaces
{
    public interface IOperationService
    {
        Task<(IEnumerable<OperationListDto> Operations, int TotalCount, decimal TotalExpenses, decimal TotalGrossSales, decimal NetProfit)>
            GetAllOperationsPaginatedAsync(int page = 1, int pageSize = 10, string? q = null, string? field = null);
            
        Task<OperationCreateDto> GetOperationByIdAsync(int id);
        Task CreateOperationAsync(OperationCreateDto dto);
        Task UpdateOperationAsync(OperationCreateDto dto);
        Task DeleteOperationAsync(int id);
    }
}
