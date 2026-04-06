using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Operation;
using AlAsma.Admin.Interfaces;
using AlAsma.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace AlAsma.Admin.Services
{
    public class OperationService : IOperationService
    {
        private readonly IUnitOfWork _uow;

        public OperationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        private static decimal CalculateTotal(decimal expenseAmount, int quantity)
        {
            return expenseAmount * quantity;
        }

        public async Task<(IEnumerable<OperationListDto> Operations, int TotalCount, decimal TotalExpenses, decimal TotalGrossSales, decimal NetProfit)>
            GetAllOperationsPaginatedAsync(int page = 1, int pageSize = 10, string? q = null, string? field = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            var operationsQuery =
                from operation in _uow.Operations.Query()
                join author in _uow.Authors.Query() on operation.AuthorId equals author.Id into authorGroup
                from author in authorGroup.DefaultIfEmpty()
                select new
                {
                    operation.Id,
                    operation.AuthorId,
                    operation.BookTitle,
                    operation.OperationName,
                    operation.ExpenseAmount,
                    operation.Quantity,
                    operation.TotalAmount,
                    operation.OperationDate,
                    AuthorName = author != null ? author.Name : null,
                    AuthorCode = author != null ? author.Code : null
                };

            if (!string.IsNullOrWhiteSpace(q))
            {
                var searchTerm = q.Trim();
                var searchPattern = $"%{searchTerm}%";

                operationsQuery = (field ?? "operation").Trim().ToLowerInvariant() switch
                {
                    "book" => operationsQuery.Where(o => EF.Functions.Like(o.BookTitle ?? string.Empty, searchPattern)),
                    "author" => operationsQuery.Where(o => EF.Functions.Like(o.AuthorName ?? string.Empty, searchPattern)),
                    "code" => operationsQuery.Where(o => EF.Functions.Like(o.AuthorCode ?? string.Empty, searchPattern)),
                    _ => operationsQuery.Where(o => EF.Functions.Like(o.OperationName, searchPattern))
                };
            }

            var totalCount = await operationsQuery.CountAsync();
            var totalOpsExpenses = await operationsQuery.Select(o => (decimal?)o.TotalAmount).SumAsync() ?? 0m;
            var totalSalesExpenses = await _uow.Sales.Query().SumAsync(s => (decimal?)(s.BasicExpenses * s.Quantity)) ?? 0m;
            var totalExpenses = totalOpsExpenses + totalSalesExpenses;
            
            var totalGrossSales = await _uow.Sales.Query().SumAsync(s => (decimal?)(s.SalePrice * s.Quantity)) ?? 0m;
            var netProfit = totalGrossSales - totalExpenses;

            var paged = await operationsQuery
                .OrderByDescending(o => o.OperationDate)
                .ThenByDescending(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OperationListDto
                {
                    Id = o.Id,
                    AuthorId = o.AuthorId,
                    OperationName = o.OperationName,
                    BookTitle = o.BookTitle == null || o.BookTitle == string.Empty ? "نفقة عامة" : o.BookTitle,
                    AuthorName = o.AuthorName ?? "بدون مؤلف",
                    AuthorCode = o.AuthorCode ?? "—",
                    ExpenseAmount = o.ExpenseAmount,
                    Quantity = o.Quantity,
                    TotalAmount = o.TotalAmount,
                    OperationDate = o.OperationDate
                })
                .ToListAsync();

            return (paged, totalCount, totalExpenses, totalGrossSales, netProfit);
        }

        public async Task<OperationCreateDto> GetOperationByIdAsync(int id)
        {
            var op = await _uow.Operations.GetByIdAsync(id);
            if (op == null) throw new Exception("العملية غير موجودة");

            return new OperationCreateDto
            {
                Id = op.Id,
                BookTitle = op.BookTitle,
                AuthorId = op.AuthorId,
                OperationName = op.OperationName,
                ExpenseAmount = op.ExpenseAmount,
                Quantity = op.Quantity
            };
        }

        public async Task CreateOperationAsync(OperationCreateDto dto)
        {
            if (dto.AuthorId.HasValue)
            {
                var authorExists = await _uow.Authors.AnyAsync(a => a.Id == dto.AuthorId.Value);
                if (!authorExists)
                {
                    throw new InvalidOperationException("لم يتم العثور على المؤلف المحدد");
                }
            }

            var op = new Operation
            {
                BookTitle = string.IsNullOrWhiteSpace(dto.BookTitle) ? null : dto.BookTitle,
                AuthorId = dto.AuthorId,
                OperationName = dto.OperationName,
                ExpenseAmount = dto.ExpenseAmount,
                Quantity = dto.Quantity,
                TotalAmount = CalculateTotal(dto.ExpenseAmount, dto.Quantity),
                OperationDate = DateTime.UtcNow
            };

            await _uow.Operations.AddAsync(op);
            await _uow.SaveChangesAsync();
        }

        public async Task UpdateOperationAsync(OperationCreateDto dto)
        {
            var op = await _uow.Operations.GetByIdAsync(dto.Id);
            if (op == null) throw new Exception("العملية غير موجودة");

            if (dto.AuthorId.HasValue)
            {
                var authorExists = await _uow.Authors.AnyAsync(a => a.Id == dto.AuthorId.Value);
                if (!authorExists)
                {
                    throw new InvalidOperationException("لم يتم العثور على المؤلف المحدد");
                }
            }

            op.BookTitle = string.IsNullOrWhiteSpace(dto.BookTitle) ? null : dto.BookTitle;
            op.AuthorId = dto.AuthorId;
            op.OperationName = dto.OperationName;
            op.ExpenseAmount = dto.ExpenseAmount;
            op.Quantity = dto.Quantity;
            op.TotalAmount = CalculateTotal(dto.ExpenseAmount, dto.Quantity);
            
            _uow.Operations.Update(op);
            await _uow.SaveChangesAsync();
        }

        public async Task DeleteOperationAsync(int id)
        {
            var op = await _uow.Operations.GetByIdAsync(id);
            if (op != null)
            {
                _uow.Operations.Delete(op);
                await _uow.SaveChangesAsync();
            }
        }
    }
}
