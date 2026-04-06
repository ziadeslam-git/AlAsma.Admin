using System;

namespace AlAsma.Admin.DTOs.Operation
{
    public class OperationListDto
    {
        public int Id { get; set; }
        public int? AuthorId { get; set; }
        public string? BookTitle { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorCode { get; set; } = string.Empty;
        public decimal ExpenseAmount { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OperationDate { get; set; }
    }
}
