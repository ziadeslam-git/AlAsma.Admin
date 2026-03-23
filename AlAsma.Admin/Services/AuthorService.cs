using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.Interfaces;
using AlAsma.Admin.Models;

namespace AlAsma.Admin.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuthorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<AuthorListDto>> GetAllAuthorsAsync()
        {
            var authors = await _unitOfWork.Authors.GetAllAsync();
            var allSales = await _unitOfWork.Sales.GetAllAsync();
            var salesByAuthor = allSales
                .Where(s => s.AuthorId > 0)
                .GroupBy(s => s.AuthorId)
                .ToDictionary(g => (int)g.Key, g => g.Count());

            return authors
                .Where(a => a.Role != "SuperAdmin" && a.Role != "Admin")
                .Select(a => new AuthorListDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Code = a.Code,
                    ContractStart = a.ContractStart,
                    ContractEnd = a.ContractEnd,
                    BasicFees = a.BasicFees,
                    ContractStatus = a.ContractStatus,
                    DaysRemaining = a.DaysRemaining,
                    SalesCount = salesByAuthor.GetValueOrDefault(a.Id, 0)
                }).ToList();
        }

        public async Task<(IEnumerable<AuthorListDto> Authors, int TotalCount)> GetAllAuthorsPaginatedAsync(int page, int pageSize = 10)
        {
            var all = await GetAllAuthorsAsync();
            var list = all.ToList();
            var total = list.Count;
            var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (paged, total);
        }

        public async Task<AuthorListDto?> GetAuthorByIdAsync(int id)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(id);
            if (author == null || author.Role == "SuperAdmin" || author.Role == "Admin")
                return null;

            return new AuthorListDto
            {
                Id = author.Id,
                Name = author.Name,
                Code = author.Code,
                ContractStart = author.ContractStart,
                ContractEnd = author.ContractEnd,
                BasicFees = author.BasicFees,
                ContractStatus = author.ContractStatus,
                DaysRemaining = author.DaysRemaining
            };
        }

        public async Task<bool> CreateAuthorAsync(AuthorCreateDto dto)
        {
            if (!await IsCodeUniqueAsync(dto.Code))
            {
                return false;
            }

            var author = new Author
            {
                Name = dto.Name,
                Code = dto.Code,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Author",
                ContractStart = dto.ContractStart,
                ContractEnd = dto.ContractEnd,
                BasicFees = dto.BasicFees,
                IsDeleted = false
            };

            await _unitOfWork.Authors.AddAsync(author);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAuthorAsync(AuthorEditDto dto)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(dto.Id);
            if (author == null || author.Role == "SuperAdmin" || author.Role == "Admin")
                return false;

            author.Name = dto.Name;
            author.ContractStart = dto.ContractStart;
            author.ContractEnd = dto.ContractEnd;
            author.BasicFees = dto.BasicFees;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                author.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            _unitOfWork.Authors.Update(author);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAuthorAsync(int id)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(id);
            if (author == null || author.Role == "SuperAdmin" || author.Role == "Admin")
                return false;

            author.IsDeleted = true;
            _unitOfWork.Authors.Update(author);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsCodeUniqueAsync(string code)
        {
            var authors = await _unitOfWork.Authors.GetAllAsync();
            return !authors.Any(a => a.Code == code);
        }
    }
}
