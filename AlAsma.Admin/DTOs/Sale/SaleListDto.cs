using System;

namespace AlAsma.Admin.DTOs.Sale
{
    public class SaleListDto
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorCode { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public decimal BasicExpenses { get; set; }
        public decimal TotalAmount { get; set; }
        public int Quantity { get; set; }
        public string StoreLocation { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
    }
}
