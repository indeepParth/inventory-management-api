namespace InventoryManagement.Application.Authorization
{
    public static class CompanyInvitationStatuses
    {
        public const string Pending = nameof(Pending);
        public const string Accepted = nameof(Accepted);
        public const string Revoked = nameof(Revoked);
        public const string Expired = nameof(Expired);

        public static readonly string[] Stored =
        [
            Pending,
            Accepted,
            Revoked
        ];
    }
}
