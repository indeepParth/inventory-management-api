namespace InventoryManagement.Domain.Entities
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int MaxCompanies { get; set; }
        public int MaxUsersPerCompany { get; set; }
        public int MaxInvoicesPerMonth { get; set; }
        public int MaxProducts { get; set; }
        public int MaxCustomers { get; set; }
        public bool IsActive { get; set; }

        public ICollection<UserSubscription> UserSubscriptions { get; set; } =
            new List<UserSubscription>();
    }
}
