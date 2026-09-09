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
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
        public ICollection<CompanyProfile> Profiles { get; set; } = new List<CompanyProfile>();
    }
}
