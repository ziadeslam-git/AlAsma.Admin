using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.DTOs.Dashboard;
using AlAsma.Admin.DTOs.Sale;
using AlAsma.Admin.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        // ─── Shared projection: Sale → SaleListDto (server-side) ────────
        private IQueryable<SaleListDto> BuildSaleProjection()
        {
            var sales = _unitOfWork.Sales.Query();
            var authors = _unitOfWork.Authors.Query();

            return from s in sales
                   join a in authors on s.AuthorId equals a.Id into ag
                   from a in ag.DefaultIfEmpty()
                   select new SaleListDto
                   {
                       Id = s.Id,
                       BookTitle = s.BookTitle,
                       AuthorId = s.AuthorId ?? 0,
                       AuthorName = a != null ? a.Name : "بدون مؤلف",
                       AuthorCode = a != null ? a.Code : "—",
                       SalePrice = s.SalePrice,
                       BasicExpenses = s.BasicExpenses,
                       TotalAmount = s.TotalAmount,
                       Quantity = s.Quantity,
                       StoreLocation = s.StoreLocation,
                       SaleDate = s.SaleDate
                   };
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync()
        {
            var utcNow = DateTime.UtcNow;
            var authorQuery = _unitOfWork.Authors.Query()
                .Where(a => a.Role == "Author");

            // DB-side aggregates — no full table materialization
            var totalAuthors = await authorQuery.CountAsync();
            var totalSalesCount = await _unitOfWork.Sales.Query().CountAsync();
            // IMPORTANT: Dashboard = Gross Sales (SalePrice x Quantity), NOT TotalAmount.
            // TotalAmount is net (after expenses) — used on Sales page only.
            // DO NOT change this back to TotalAmount.
            // Dashboard shows GROSS sales (before deducting BasicExpenses)
            var totalRevenue = await _unitOfWork.Sales.Query()
                .SumAsync(s => (decimal?)(s.SalePrice * s.Quantity)) ?? 0m;

            // Contract status counts using ContractEnd directly (DB-side)
            // null ContractEnd = "غير محدد" — not counted in any of the three
            var activeContracts = await authorQuery
                .CountAsync(a => a.ContractEnd != null && a.ContractEnd > utcNow.AddDays(20));
            var endingSoonContracts = await authorQuery
                .CountAsync(a => a.ContractEnd != null && a.ContractEnd > utcNow && a.ContractEnd <= utcNow.AddDays(20));
            var expiredContracts = await authorQuery
                .CountAsync(a => a.ContractEnd != null && a.ContractEnd <= utcNow);

            // Recent sales — only latest 5 from DB
            var recentSales = await BuildSaleProjection()
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .Take(5)
                .ToListAsync();

            // Expiring authors — only matching ones, materialized then mapped
            var expiringRaw = await authorQuery
                .Where(a => a.ContractEnd != null && a.ContractEnd > utcNow && a.ContractEnd <= utcNow.AddDays(20))
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Code,
                    a.ContractStart,
                    a.ContractEnd,
                    a.BasicFees
                })
                .ToListAsync();

            var expiringAuthors = expiringRaw.Select(a => new AuthorListDto
            {
                Id = a.Id,
                Name = a.Name,
                Code = a.Code,
                ContractStart = a.ContractStart,
                ContractEnd = a.ContractEnd,
                BasicFees = a.BasicFees,
                ContractStatus = "ينتهي قريباً",
                DaysRemaining = a.ContractEnd.HasValue
                    ? Math.Max(0, (int)(a.ContractEnd.Value - utcNow).TotalDays)
                    : null
            }).ToList();

            return new AdminDashboardDto
            {
                TotalAuthors = totalAuthors,
                TotalSales = totalSalesCount,
                TotalRevenue = totalRevenue,
                ActiveContracts = activeContracts,
                EndingSoonContracts = endingSoonContracts,
                ExpiredContracts = expiredContracts,
                RecentSales = recentSales,
                ExpiringAuthors = expiringAuthors
            };
        }

        public async Task<AuthorDashboardDto?> GetAuthorDashboardAsync(int authorId, int salesPage = 1, int operationsPage = 1)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(authorId);
            if (author == null) return null;

            salesPage = Math.Max(1, salesPage);
            operationsPage = Math.Max(1, operationsPage);

            // DB aggregates — no row materialization for totals
            var totalSales = await _unitOfWork.Sales.Query()
                .Where(s => s.AuthorId == authorId)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            var salesCount = await _unitOfWork.Sales.Query()
                .Where(s => s.AuthorId == authorId)
                .CountAsync();

            const int pageSize = 10;
            // Dashboard sales with server-side pagination
            var recentSales = await BuildSaleProjection()
                .Where(s => s.AuthorId == authorId)
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .Skip((salesPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var opsExpenses = await _unitOfWork.Operations.Query()
                .Where(o => o.AuthorId == authorId)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var opsCount = await _unitOfWork.Operations.Query()
                .Where(o => o.AuthorId == authorId)
                .CountAsync();

            var recentOperations = await _unitOfWork.Operations.Query()
                .Where(o => o.AuthorId == authorId)
                .OrderByDescending(o => o.OperationDate)
                .ThenByDescending(o => o.Id)
                .Skip((operationsPage - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AlAsma.Admin.DTOs.Operation.OperationListDto
                {
                    Id = o.Id,
                    OperationName = o.OperationName,
                    BookTitle = o.BookTitle ?? "عملية عامة",
                    ExpenseAmount = o.ExpenseAmount,
                    Quantity = o.Quantity,
                    TotalAmount = o.TotalAmount,
                    OperationDate = o.OperationDate
                })
                .ToListAsync();

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
                OperationsExpenses = opsExpenses,
                OperationsCount = opsCount,
                NetProfit = totalSales - author.BasicFees - opsExpenses,
                SalesCount = salesCount,
                SalesCurrentPage = salesPage,
                SalesTotalPages = (int)Math.Ceiling(salesCount / (double)pageSize),
                OperationsCurrentPage = operationsPage,
                OperationsTotalPages = (int)Math.Ceiling(opsCount / (double)pageSize),
                RecentSales = recentSales,
                RecentOperations = recentOperations
            };
        }

        public async Task<AuthorSalesExportDto?> GetAuthorSalesExportAsync(int authorId)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(authorId);
            if (author == null) return null;

            // DB aggregates
            var totalSales = await _unitOfWork.Sales.Query()
                .Where(s => s.AuthorId == authorId)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            var salesCount = await _unitOfWork.Sales.Query()
                .Where(s => s.AuthorId == authorId)
                .CountAsync();

            // FULL sales history — export-specific, not for dashboard rendering
            var allSales = await BuildSaleProjection()
                .Where(s => s.AuthorId == authorId)
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .ToListAsync();

            var opsExpenses = await _unitOfWork.Operations.Query()
                .Where(o => o.AuthorId == authorId)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var opsCount = await _unitOfWork.Operations.Query()
                .Where(o => o.AuthorId == authorId)
                .CountAsync();

            var allOperations = await _unitOfWork.Operations.Query()
                .Where(o => o.AuthorId == authorId)
                .OrderByDescending(o => o.OperationDate)
                .ThenByDescending(o => o.Id)
                .Select(o => new AlAsma.Admin.DTOs.Operation.OperationListDto
                {
                    Id = o.Id,
                    OperationName = o.OperationName,
                    BookTitle = o.BookTitle ?? "عملية عامة",
                    ExpenseAmount = o.ExpenseAmount,
                    Quantity = o.Quantity,
                    TotalAmount = o.TotalAmount,
                    OperationDate = o.OperationDate
                })
                .ToListAsync();

            return new AuthorSalesExportDto
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
                OperationsExpenses = opsExpenses,
                OperationsCount = opsCount,
                NetProfit = totalSales - author.BasicFees - opsExpenses,
                SalesCount = salesCount,
                Sales = allSales,
                Operations = allOperations
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
                // IMPORTANT: Dashboard = Gross Sales (SalePrice x Quantity), NOT TotalAmount.
                // TotalAmount is net (after expenses) — used on Sales page only.
                // DO NOT change this back to TotalAmount.
                // Dashboard shows GROSS sales (before deducting BasicExpenses)
                TotalRevenue = allSales.Sum(s => s.SalePrice * s.Quantity),
                AdminsList = adminsList
            };
        }
    }
}
