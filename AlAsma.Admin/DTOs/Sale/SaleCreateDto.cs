using System;
using System.ComponentModel.DataAnnotations;

namespace AlAsma.Admin.DTOs.Sale
{
    public class SaleCreateDto
    {
        public int Id { get; set; } // 0 = new sale, >0 = edit

        [Required(ErrorMessage = "اسم الكتاب مطلوب")]
        [MaxLength(200)]
        public string BookTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "التأليف مطلوب")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار مؤلف")]
        public int AuthorId { get; set; }

        [Required(ErrorMessage = "السعر مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
        public decimal SalePrice { get; set; }

        [Required(ErrorMessage = "المصروفات مطلوبة")]
        [Range(0, double.MaxValue, ErrorMessage = "المصروفات لا يمكن أن تكون سالبة")]
        public decimal BasicExpenses { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون 1 على الأقل")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "المنفذ مطلوب")]
        [MaxLength(200)]
        public string StoreLocation { get; set; } = string.Empty;

        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    }
}
