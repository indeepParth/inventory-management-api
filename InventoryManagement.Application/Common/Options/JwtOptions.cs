namespace InventoryManagement.Application.Common.Options
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";
        public const int MinimumSigningKeyBytes = 32;

        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenLifetimeMinutes { get; set; } = 60;
    }
}
