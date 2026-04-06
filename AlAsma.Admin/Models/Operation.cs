using System;
using System.ComponentModel.DataAnnotations;

namespace AlAsma.Admin.Models
{
    public class Operation : BaseEntity
    {
        [MaxLength(255)]
        public string? BookTitle { get; set; }

        public int? AuthorId { get; set; }

        [Required]
        [MaxLength(255)]
        public string OperationName { get; set; } = string.Empty;

        public decimal ExpenseAmount { get; set; }

        public int Quantity { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime OperationDate { get; set; }

        public virtual Author? Author { get; set; }
    }
}
