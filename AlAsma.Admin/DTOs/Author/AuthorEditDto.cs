using System;
using System.ComponentModel.DataAnnotations;

namespace AlAsma.Admin.DTOs.Author
{
    public class AuthorEditDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Password { get; set; }

        public DateTime? ContractStart { get; set; }

        public DateTime? ContractEnd { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "المصاريف الأساسية يجب أن تكون رقمًا موجبًا")]
        public decimal BasicFees { get; set; }
    }
}
