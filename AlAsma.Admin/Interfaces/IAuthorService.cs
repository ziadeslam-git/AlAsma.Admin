using System.Collections.Generic;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Author;

namespace AlAsma.Admin.Interfaces
{
    public interface IAuthorService
    {
        Task<IEnumerable<AuthorListDto>> GetAllAuthorsAsync();
        Task<(IEnumerable<AuthorListDto> Authors, int TotalCount)> GetAllAuthorsPaginatedAsync(int page, int pageSize = 10);
        Task<AuthorListDto?> GetAuthorByIdAsync(int id);
        Task<bool> CreateAuthorAsync(AuthorCreateDto dto);
        Task<bool> UpdateAuthorAsync(AuthorEditDto dto);
        Task<bool> SoftDeleteAuthorAsync(int id);
        Task<bool> IsCodeUniqueAsync(string code);
    }
}
