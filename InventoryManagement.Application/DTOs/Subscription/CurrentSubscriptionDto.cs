namespace InventoryManagement.Application.DTOs.Subscription
{
    public class CurrentSubscriptionDto
    {
        public string Status { get; set; } = string.Empty;
        public SubscriptionPlanDto Plan { get; set; } = new();
        public SubscriptionUsageDto Usage { get; set; } = new();
    }

    public class SubscriptionPlanDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int MaxCompanies { get; set; }
        public int MaxUsersPerCompany { get; set; }
        public int MaxInvoicesPerMonth { get; set; }
        public int MaxProducts { get; set; }
        public int MaxCustomers { get; set; }
    }

    public class SubscriptionUsageDto
    {
        public int OwnedCompanies { get; set; }
        public int? CompanyUsers { get; set; }
        public int? InvoicesThisMonth { get; set; }
        public int? Products { get; set; }
        public int? Customers { get; set; }
    }
}
