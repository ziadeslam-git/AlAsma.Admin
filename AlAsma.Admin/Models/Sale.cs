using System;
using System.ComponentModel.DataAnnotations;

namespace AlAsma.Admin.Models
{
    public class Sale : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string BookTitle { get; set; } = null!;

        public int? AuthorId { get; set; }

        // Navigation (optional to allow sales to remain when an author is soft-deleted)
        public Author? Author { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal SalePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BasicExpenses { get; set; }

        // Must be set by SaleService, not the model
        public decimal TotalAmount { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public string StoreLocation { get; set; } = null!;

        public DateTime SaleDate { get; set; }
    }
}
