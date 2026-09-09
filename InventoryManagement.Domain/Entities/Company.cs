using System;
using System.Collections.Generic;

namespace InventoryManagement.Domain.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }

        public ICollection<CompanyUser> Users { get; set; } = new List<CompanyUser>();
    }
}
