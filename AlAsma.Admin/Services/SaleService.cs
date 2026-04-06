using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Sale;
using AlAsma.Admin.Interfaces;
using AlAsma.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace AlAsma.Admin.Services
{
    public class SaleService : ISaleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SaleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// TotalAmount = (SalePrice × Quantity) - (BasicExpenses × Quantity)
        /// BasicExpenses is a per-unit cost (printing, shipping, etc.) — multiplied by each copy sold.
        /// </summary>
        private decimal CalculateTotal(decimal salePrice, int quantity, decimal basicExpenses)
        {
            var revenue = salePrice * quantity;
            var totalExpenses = basicExpenses * quantity;
            return Math.Max(0, revenue - totalExpenses);
        }

        // ─── Shared projection helper ───────────────────────────────────
        // Builds a server-side left join between Sales and Authors,
        // projecting directly to SaleListDto. Reused by all read methods.
        private IQueryable<SaleListDto> BuildSaleProjectionQuery()
        {
            var sales = _unitOfWork.Sales.Query();
            var authors = _unitOfWork.Authors.Query();

            return from s in sales
                   join a in authors on s.AuthorId equals a.Id into authorGroup
                   from a in authorGroup.DefaultIfEmpty()
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

        public async Task<IEnumerable<SaleListDto>> GetAllSalesAsync()
        {
            return await BuildSaleProjectionQuery()
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .ToListAsync();
        }

        // This method is the SQL-level implementation of pagination/filtering/aggregates.
        // It intentionally uses the existing repository Query() abstraction and
        // does NOT require introducing GetQueryable().
        public async Task<(IEnumerable<SaleListDto> Sales, int TotalCount, decimal TotalRevenue, decimal TotalExpenses, int TotalQuantity)>
            GetAllSalesPaginatedAsync(int page, int pageSize = 10, string? q = null, string? searchField = null)
        {
            // Start from the shared server-side projection (Sales LEFT JOIN Authors → SaleListDto)
            var query = BuildSaleProjectionQuery();

            // Apply search filter in SQL, not in memory
            if (!string.IsNullOrWhiteSpace(q))
            {
                var lq = q.Trim().ToLower();
                query = searchField switch
                {
                    "author" => query.Where(s => s.AuthorName.ToLower().Contains(lq)),
                    "code"   => query.Where(s => s.AuthorCode.ToLower().Contains(lq)),
                    "store"  => query.Where(s => s.StoreLocation.ToLower().Contains(lq)),
                    _        => query.Where(s => s.BookTitle.ToLower().Contains(lq))
                };
            }

            // COUNT + aggregates in SQL (not in memory)
            var totalCount = await query.CountAsync();

            // SALES PAGE: TotalRevenue = Net revenue (TotalAmount = SalePrice×Qty − BasicExpenses×Qty)
            // This is DIFFERENT from Dashboard which uses Gross (SalePrice × Quantity).
            // Do NOT confuse the two — see DashboardService for gross calculation.
            var totalRevenue  = await query.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

            // TotalExpenses = sum of per-unit expenses × quantity for all filtered sales
            var totalExpenses = await query.SumAsync(s => (decimal?)(s.BasicExpenses * s.Quantity)) ?? 0m;

            var totalQuantity = await query.SumAsync(s => (int?)s.Quantity) ?? 0;

            // Paginate in SQL — only fetch the rows needed
            var paged = await query
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (paged, totalCount, totalRevenue, totalExpenses, totalQuantity);
        }

        public async Task<IEnumerable<SaleListDto>> GetSalesByAuthorAsync(int authorId)
        {
            return await BuildSaleProjectionQuery()
                .Where(s => s.AuthorId == authorId)
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .ToListAsync();
        }

        public async Task<SaleListDto?> GetSaleByIdAsync(int id)
        {
            var sale = await _unitOfWork.Sales.GetByIdAsync(id);
            if (sale == null) return null;

            var author = await _unitOfWork.Authors.GetByIdAsync(sale.AuthorId ?? 0);

            return new SaleListDto
            {
                Id = sale.Id,
                BookTitle = sale.BookTitle,
                AuthorId = sale.AuthorId ?? 0,
                AuthorName = author?.Name ?? "بدون مؤلف",
                AuthorCode = author?.Code ?? "—",
                SalePrice = sale.SalePrice,
                BasicExpenses = sale.BasicExpenses,
                TotalAmount = sale.TotalAmount,
                Quantity = sale.Quantity,
                StoreLocation = sale.StoreLocation,
                SaleDate = sale.SaleDate
            };
        }

        public async Task<bool> CreateSaleAsync(SaleCreateDto dto)
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(dto.AuthorId);
            if (author == null || author.IsDeleted)
            {
                return false;
            }

            var total = CalculateTotal(dto.SalePrice, dto.Quantity, dto.BasicExpenses);

            var sale = new Sale
            {
                BookTitle = dto.BookTitle,
                AuthorId = dto.AuthorId,
                SalePrice = dto.SalePrice,
                BasicExpenses = dto.BasicExpenses,
                Quantity = dto.Quantity,
                StoreLocation = dto.StoreLocation,
                SaleDate = dto.SaleDate,
                TotalAmount = total
            };

            await _unitOfWork.Sales.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSaleAsync(int id)
        {
            var sale = await _unitOfWork.Sales.GetByIdAsync(id);
            if (sale == null) return false;

            _unitOfWork.Sales.Delete(sale); // Hard delete
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSaleAsync(SaleCreateDto dto)
        {
            var sale = await _unitOfWork.Sales.GetByIdAsync(dto.Id);
            if (sale == null) return false;

            sale.BookTitle = dto.BookTitle;
            sale.AuthorId = dto.AuthorId;
            sale.StoreLocation = dto.StoreLocation;
            sale.SalePrice = dto.SalePrice;
            sale.Quantity = dto.Quantity;
            sale.BasicExpenses = dto.BasicExpenses;
            sale.TotalAmount = CalculateTotal(dto.SalePrice, dto.Quantity, dto.BasicExpenses);

            _unitOfWork.Sales.Update(sale);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalSalesByAuthorAsync(int authorId)
        {
            // DB aggregate — no row materialization
            return await _unitOfWork.Sales.Query()
                .Where(s => s.AuthorId == authorId)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
        }

        public async Task<decimal> GetNetProfitByAuthorAsync(int authorId)
        {
            var totalSales = await GetTotalSalesByAuthorAsync(authorId);
            var author = await _unitOfWork.Authors.GetByIdAsync(authorId);

            if (author == null) return 0;

            return totalSales - author.BasicFees;
        }
    }
}
