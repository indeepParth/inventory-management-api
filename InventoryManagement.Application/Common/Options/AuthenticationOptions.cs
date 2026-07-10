namespace InventoryManagement.Application.Common.Options
{
    public sealed class AuthenticationOptions
    {
        public const string SectionName = "Authentication";

        public bool AllowPublicRegistration { get; set; }
    }
}
