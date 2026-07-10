namespace InventoryManagement.API.Configuration
{
    public sealed class ApiRateLimitOptions
    {
        public const string SectionName = "RateLimiting";

        public FixedWindowRateLimitOptions Global { get; set; } = new()
        {
            PermitLimit = 120,
            WindowSeconds = 60
        };

        public FixedWindowRateLimitOptions Login { get; set; } = new()
        {
            PermitLimit = 5,
            WindowSeconds = 60
        };

        public FixedWindowRateLimitOptions Register { get; set; } = new()
        {
            PermitLimit = 3,
            WindowSeconds = 60
        };

        public FixedWindowRateLimitOptions RefreshToken { get; set; } = new()
        {
            PermitLimit = 10,
            WindowSeconds = 60
        };
    }

    public sealed class FixedWindowRateLimitOptions
    {
        public int PermitLimit { get; set; }
        public int WindowSeconds { get; set; }
        public int QueueLimit { get; set; }

        public int SafePermitLimit => PermitLimit > 0 ? PermitLimit : 1;
        public int SafeWindowSeconds => WindowSeconds > 0 ? WindowSeconds : 60;
        public int SafeQueueLimit => QueueLimit >= 0 ? QueueLimit : 0;
    }
}
