using System;

namespace AlAsma.Admin.DTOs.Author
{
    public class AuthorListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime? ContractStart { get; set; }
        public DateTime? ContractEnd { get; set; }
        public decimal BasicFees { get; set; }
        
        public string ContractStatus { get; set; } = string.Empty;
        public int? DaysRemaining { get; set; }
        public int SalesCount { get; set; }
    }
}
