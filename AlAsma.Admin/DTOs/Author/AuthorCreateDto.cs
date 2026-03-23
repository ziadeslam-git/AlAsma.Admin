using System;
using System.ComponentModel.DataAnnotations;

namespace AlAsma.Admin.DTOs.Author
{
    public class AuthorCreateDto
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "الكود مطلوب")]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        public DateTime? ContractStart { get; set; }

        public DateTime? ContractEnd { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "المصاريف الأساسية يجب أن تكون رقمًا موجبًا")]
        public decimal BasicFees { get; set; }
    }
}
