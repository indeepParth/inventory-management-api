namespace InventoryManagement.Domain.Entities
{
    public class UserSubscription
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int PlanId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public SubscriptionPlan Plan { get; set; } = null!;
    }
}
