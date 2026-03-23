using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.DTOs.Dashboard;
using AlAsma.Admin.DTOs.Sale;
using AlAsma.Admin.Interfaces;

namespace AlAsma.Admin.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISaleService _saleService;
        private readonly IAuthorService _authorService;

        public DashboardService(IUnitOfWork unitOfWork, ISaleService saleService, IAuthorService authorService)
        {
            _unitOfWork = unitOfWork;
            _saleService = saleService;
            _authorService = authorService;
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync()
        {
            // Get authors with Role == "Author" only
            var allAuthorsRaw = await _unitOfWork.Authors.GetAllAsync();
            var authors = allAuthorsRaw
                .Where(a => a.Role == "Author")
                .Select(a => new AuthorListDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Code = a.Code,
                    ContractStart = a.ContractStart,
                    ContractEnd = a.ContractEnd,
                    BasicFees = a.BasicFees,
                    ContractStatus = a.ContractStatus,
                    DaysRemaining = a.DaysRemaining
                }).ToList();

            // Get all sales
            var allSales = (await _saleService.GetAllSalesAsync()).ToList();

            return new AdminDashboardDto
            {
                TotalAuthors = authors.Count,
                TotalSales = allSales.Count,
                TotalRevenue = allSales.Sum(s => s.TotalAmount),
                ActiveContracts = authors.Count(a => a.ContractStatus == "نشط"),
                EndingSoonContracts = authors.Count(a => a.ContractStatus == "ينتهي قريباً"),
                ExpiredContracts = authors.Count(a => a.ContractStatus == "منتهي"),
                RecentSales = allSales.OrderByDescending(s => s.SaleDate).Take(5).ToList(),
                ExpiringAuthors = authors.Where(a => a.ContractStatus == "ينتهي قريباً").ToList()
            };
        }

        public async Task<AuthorDashboardDto?> GetAuthorDashboardAsync(int authorId)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(authorId);
            if (author == null) return null;

            var sales = (await _saleService.GetSalesByAuthorAsync(authorId)).ToList();
            var totalSales = sales.Sum(s => s.TotalAmount);

            return new AuthorDashboardDto
            {
                AuthorId = author.Id,
                AuthorName = author.Name,
                AuthorCode = author.Code,
                ContractStart = author.ContractStart,
                ContractEnd = author.ContractEnd,
                ContractStatus = author.ContractStatus,
                DaysRemaining = author.DaysRemaining,
                BasicFees = author.BasicFees,
                TotalSales = totalSales,
                NetProfit = totalSales - author.BasicFees,
                SalesCount = sales.Count,
                RecentSales = sales.OrderByDescending(s => s.SaleDate).Take(5).ToList()
            };
        }

        public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
        {
            var allAuthors = await _unitOfWork.Authors.GetAllAsync();
            var allSales = await _unitOfWork.Sales.GetAllAsync();

            var adminsList = allAuthors
                .Where(a => a.Role == "Admin")
                .Select(a => new AuthorListDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Code = a.Code,
                    ContractStart = a.ContractStart,
                    ContractEnd = a.ContractEnd,
                    BasicFees = a.BasicFees,
                    ContractStatus = a.ContractStatus,
                    DaysRemaining = a.DaysRemaining
                }).ToList();

            return new SuperAdminDashboardDto
            {
                TotalAdmins = adminsList.Count,
                TotalAuthors = allAuthors.Count(a => a.Role == "Author"),
                TotalRevenue = allSales.Sum(s => s.TotalAmount),
                AdminsList = adminsList
            };
        }
    }
}
