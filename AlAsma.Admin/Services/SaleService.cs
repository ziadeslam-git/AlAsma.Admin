using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlAsma.Admin.DTOs.Sale;
using AlAsma.Admin.Interfaces;
using AlAsma.Admin.Models;

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

        public async Task<IEnumerable<SaleListDto>> GetAllSalesAsync()
        {
            var sales = await _unitOfWork.Sales.GetAllAsync();
            var authors = await _unitOfWork.Authors.GetAllAsync();

            return sales.Select(s => 
            {
                var author = authors.FirstOrDefault(a => a.Id == s.AuthorId);
                return new SaleListDto
                {
                    Id = s.Id,
                    BookTitle = s.BookTitle,
                    AuthorId = s.AuthorId ?? 0,
                    AuthorName = author?.Name ?? "بدون مؤلف",
                    AuthorCode = author?.Code ?? "—",
                    SalePrice = s.SalePrice,
                    BasicExpenses = s.BasicExpenses,
                    TotalAmount = s.TotalAmount,
                    Quantity = s.Quantity,
                    StoreLocation = s.StoreLocation,
                    SaleDate = s.SaleDate
                };
            }).OrderByDescending(s => s.SaleDate).ToList();
        }

        public async Task<(IEnumerable<SaleListDto> Sales, int TotalCount)> GetAllSalesPaginatedAsync(int page, int pageSize = 10)
        {
            var all = await _unitOfWork.Sales.GetAllAsync();
            var authors = await _unitOfWork.Authors.GetAllAsync();
            
            var allSales = all.OrderByDescending(s => s.SaleDate).ToList();
            var totalCount = allSales.Count;

            var paged = allSales
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => 
                {
                    var author = authors.FirstOrDefault(a => a.Id == s.AuthorId);
                    return new SaleListDto
                    {
                        Id = s.Id,
                        BookTitle = s.BookTitle,
                        AuthorId = s.AuthorId ?? 0,
                        AuthorName = author?.Name ?? "بدون مؤلف",
                        AuthorCode = author?.Code ?? "—",
                        SalePrice = s.SalePrice,
                        BasicExpenses = s.BasicExpenses,
                        TotalAmount = s.TotalAmount,
                        Quantity = s.Quantity,
                        StoreLocation = s.StoreLocation,
                        SaleDate = s.SaleDate
                    };
                })
                .ToList();

            return (paged, totalCount);
        }

        public async Task<IEnumerable<SaleListDto>> GetSalesByAuthorAsync(int authorId)
        {
            var allSales = await GetAllSalesAsync();
            return allSales.Where(s => s.AuthorId == authorId).ToList();
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
            var sales = await GetSalesByAuthorAsync(authorId);
            return sales.Sum(s => s.TotalAmount);
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
