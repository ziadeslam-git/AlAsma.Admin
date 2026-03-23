using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlAsma.Admin.Models
{
    public class Author : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(100)]
        [Required]
        public string Code { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!; // Stored as BCrypt hash

        [Required]
        public string Role { get; set; } = "Author"; // "SuperAdmin" / "Admin" / "Author"

        public DateTime? ContractStart { get; set; }

        public DateTime? ContractEnd { get; set; }

        [Required]
        public decimal BasicFees { get; set; } = 0m;

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();

        // Calculated properties - not stored in DB
        [NotMapped]
        public string ContractStatus
        {
            get
            {
                if (!ContractEnd.HasValue) return "غير محدد";
                var days = (ContractEnd.Value - DateTime.UtcNow).TotalDays;
                if (days <= 0) return "منتهي";
                if (days <= 20) return "ينتهي قريباً";
                return "نشط";
            }
        }

        [NotMapped]
        public int? DaysRemaining
        {
            get
            {
                if (!ContractEnd.HasValue) return null;
                var days = (int)(ContractEnd.Value - DateTime.UtcNow).TotalDays;
                return days < 0 ? 0 : days;
            }
        }
    }
}
