namespace InventoryManagement.Application.Authorization
{
    public static class SubscriptionStatuses
    {
        public const string Active = nameof(Active);
        public const string Trialing = nameof(Trialing);
        public const string PastDue = nameof(PastDue);
        public const string Cancelled = nameof(Cancelled);
        public const string Expired = nameof(Expired);

        public static readonly IReadOnlyCollection<string> ActiveStatuses =
        [
            Active,
            Trialing
        ];
    }
}
