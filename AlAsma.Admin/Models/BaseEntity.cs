using System;

namespace AlAsma.Admin.Models
{
    // Abstract base entity that other entities inherit from
    public abstract class BaseEntity
    {
        // Primary Key
        public int Id { get; set; }

        // Set automatically when the entity is added
        public DateTime CreatedAt { get; set; }

        // Updated automatically on each modification
        public DateTime UpdatedAt { get; set; }
    }
}
