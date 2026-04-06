using System;
using System.ComponentModel.DataAnnotations;

namespace AlAsma.Admin.DTOs.Operation
{
    public class OperationCreateDto
    {
        public int Id { get; set; }

        [MaxLength(255)]
        public string? BookTitle { get; set; }

        public int? AuthorId { get; set; }

        [Required(ErrorMessage = "اسم العملية مطلوب")]
        [MaxLength(255)]
        public string OperationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "قيمة المصروف مطلوبة")]
        [Range(0, double.MaxValue, ErrorMessage = "يجب أن تكون قيمة المصروف موجبة")]
        public decimal ExpenseAmount { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب أن تكون الكمية 1 على الأقل")]
        public int Quantity { get; set; }
    }
}
