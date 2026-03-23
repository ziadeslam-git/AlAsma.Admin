using System.ComponentModel.DataAnnotations;

namespace AlAsma.Admin.DTOs.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "الكود مطلوب")]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}
