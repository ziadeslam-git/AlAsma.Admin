using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.Interfaces;
using AlAsma.Admin.Models;
using Microsoft.EntityFrameworkCore;

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
            // DB-side query: filter roles, project with SalesCount subquery
            // ContractStatus and DaysRemaining are [NotMapped] — computed client-side after materialization
            var authors = await _unitOfWork.Authors.Query()
                .Where(a => a.Role != "SuperAdmin" && a.Role != "Admin")
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Code,
                    a.ContractStart,
                    a.ContractEnd,
                    a.BasicFees,
                    // EF translates this to a correlated subquery in SQL
                    SalesCount = _unitOfWork.Sales.Query().Count(s => s.AuthorId == a.Id)
                })
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            // Map to DTO with computed [NotMapped] properties (client-side)
            return authors.Select(a => new AuthorListDto
            {
                Id = a.Id,
                Name = a.Name,
                Code = a.Code,
                ContractStart = a.ContractStart,
                ContractEnd = a.ContractEnd,
                BasicFees = a.BasicFees,
                ContractStatus = ComputeContractStatus(a.ContractEnd),
                DaysRemaining = ComputeDaysRemaining(a.ContractEnd),
                SalesCount = a.SalesCount
            }).ToList();
        }

        public async Task<(IEnumerable<AuthorListDto> Authors, int TotalCount)> GetAllAuthorsPaginatedAsync(int page, int pageSize = 10)
        {
            var baseQuery = _unitOfWork.Authors.Query()
                .Where(a => a.Role != "SuperAdmin" && a.Role != "Admin");

            var totalCount = await baseQuery.CountAsync();

            // DB-side pagination with stable sort — no materialization before Skip/Take
            var authors = await baseQuery
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Code,
                    a.ContractStart,
                    a.ContractEnd,
                    a.BasicFees,
                    SalesCount = _unitOfWork.Sales.Query().Count(s => s.AuthorId == a.Id)
                })
                .ToListAsync();

            var paged = authors.Select(a => new AuthorListDto
            {
                Id = a.Id,
                Name = a.Name,
                Code = a.Code,
                ContractStart = a.ContractStart,
                ContractEnd = a.ContractEnd,
                BasicFees = a.BasicFees,
                ContractStatus = ComputeContractStatus(a.ContractEnd),
                DaysRemaining = ComputeDaysRemaining(a.ContractEnd),
                SalesCount = a.SalesCount
            }).ToList();

            return (paged, totalCount);
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
            // Server-side check — no full table load
            var exists = await _unitOfWork.Authors.AnyAsync(a => a.Code == code);
            return !exists;
        }

        // ─── Client-side helpers for [NotMapped] computed properties ────
        private static string ComputeContractStatus(DateTime? contractEnd)
        {
            if (!contractEnd.HasValue) return "غير محدد";
            var days = (contractEnd.Value - DateTime.UtcNow).TotalDays;
            if (days <= 0) return "منتهي";
            if (days <= 20) return "ينتهي قريباً";
            return "نشط";
        }

        private static int? ComputeDaysRemaining(DateTime? contractEnd)
        {
            if (!contractEnd.HasValue) return null;
            var days = (int)(contractEnd.Value - DateTime.UtcNow).TotalDays;
            return days < 0 ? 0 : days;
        }
    }
}
