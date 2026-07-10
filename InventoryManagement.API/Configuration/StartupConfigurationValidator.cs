using System.Text;
using InventoryManagement.Application.Common.Options;

namespace InventoryManagement.API.Configuration
{
    public static class StartupConfigurationValidator
    {
        private const string DefaultConnectionName = "ConnectionStrings:DefaultConnection";
        private const string AllowedOriginsName = "Cors:AllowedOrigins";

        public static void Validate(
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            if (!environment.IsProduction())
            {
                return;
            }

            var errors = new List<string>();

            RequireValue(
                configuration.GetConnectionString("DefaultConnection"),
                DefaultConnectionName,
                errors);

            RequireValue(
                configuration["Jwt:Issuer"],
                "Jwt:Issuer",
                errors);

            RequireValue(
                configuration["Jwt:Audience"],
                "Jwt:Audience",
                errors);

            ValidateJwtKey(configuration["Jwt:Key"], errors);
            ValidateAllowedOrigins(configuration, errors);
            errors.AddRange(
                ForwardedHeadersConfiguration.ValidateProductionConfiguration(
                    configuration));

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Production configuration is invalid: " +
                    string.Join("; ", errors));
            }
        }

        private static void RequireValue(
            string? value,
            string settingName,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{settingName} is required.");
            }
        }

        private static void ValidateJwtKey(
            string? jwtKey,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                errors.Add("Jwt:Key is required. Set Jwt__Key in configuration or environment variables.");
                return;
            }

            if (Encoding.UTF8.GetByteCount(jwtKey) < JwtOptions.MinimumSigningKeyBytes)
            {
                errors.Add($"Jwt:Key must be at least {JwtOptions.MinimumSigningKeyBytes} UTF-8 bytes.");
            }
        }

        private static void ValidateAllowedOrigins(
            IConfiguration configuration,
            List<string> errors)
        {
            var origins = configuration
                .GetSection(AllowedOriginsName)
                .Get<string[]>() ?? [];

            var configuredOrigins = origins
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .ToArray();

            if (configuredOrigins.Length == 0)
            {
                errors.Add($"{AllowedOriginsName} must contain at least one origin.");
                return;
            }

            if (configuredOrigins.Any(origin => !IsHttpsOrigin(origin)))
            {
                errors.Add($"{AllowedOriginsName} must contain only absolute HTTPS origins in Production.");
            }
        }

        private static bool IsHttpsOrigin(string origin)
        {
            return Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                   uri.Scheme == Uri.UriSchemeHttps &&
                   !string.IsNullOrWhiteSpace(uri.Host) &&
                   uri.AbsolutePath == "/" &&
                   string.IsNullOrEmpty(uri.Query) &&
                   string.IsNullOrEmpty(uri.Fragment);
        }
    }
}
