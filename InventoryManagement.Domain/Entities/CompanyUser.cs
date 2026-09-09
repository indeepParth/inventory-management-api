using System;

namespace InventoryManagement.Domain.Entities
{
    public class CompanyUser
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }

        public Company Company { get; set; } = null!;
    }
}
