using FluentAssertions;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class ProductionConfigurationAuditTests
    {
        [Fact]
        public async Task Production_Compose_Should_Use_Production_And_Safe_Operational_Defaults()
        {
            var compose = await File.ReadAllTextAsync(
                Path.Combine(
                    GetRepositoryRoot(),
                    "docker-compose.production.yml"));

            compose.Should().Contain("ASPNETCORE_ENVIRONMENT: Production");
            compose.Should().NotContain("ASPNETCORE_ENVIRONMENT: Development");
            compose.Should().Contain("Authentication__AllowPublicRegistration: \"false\"");
            compose.Should().Contain("Database__ApplyMigrationsOnStartup: \"false\"");
            compose.Should().Contain("Swagger__Enabled: \"false\"");
            compose.Should().Contain("/health/ready");
            compose.Should().Contain("restart: unless-stopped");
            compose.Should().Contain("inventory_data:/app/Data");
            compose.Should().Contain("inventory_logs:/app/Logs");
            compose.Should().NotContain("ports:");
        }

        [Fact]
        public async Task Production_Compose_Should_Not_Embed_Secret_Values()
        {
            var compose = await File.ReadAllTextAsync(
                Path.Combine(
                    GetRepositoryRoot(),
                    "docker-compose.production.yml"));

            compose.Should().Contain("Jwt__Key: ${Jwt__Key:?Set Jwt__Key}");
            compose.Should().Contain("Bootstrap__Admin__Password: ${Bootstrap__Admin__Password:-}");
            compose.Should().NotContain("ThisIsMyTempSuperSecretKey");
            compose.Should().NotContain("admin1123");
        }

        [Fact]
        public async Task Production_Appsettings_Should_Not_Contain_Production_Secrets()
        {
            var appsettings = await File.ReadAllTextAsync(
                Path.Combine(
                    GetRepositoryRoot(),
                    "InventoryManagement.API",
                    "appsettings.json"));
            var productionAppsettings = await File.ReadAllTextAsync(
                Path.Combine(
                    GetRepositoryRoot(),
                    "InventoryManagement.API",
                    "appsettings.Production.json"));

            appsettings.Should().NotContain("Jwt:Key");
            appsettings.Should().NotContain("ThisIsMyTempSuperSecretKey");
            productionAppsettings.Should().NotContain("Jwt");
            productionAppsettings.Should().NotContain("Password");
            productionAppsettings.Should().NotContain("Secret");
        }

        private static string GetRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null &&
                   !File.Exists(Path.Combine(directory.FullName, "InventoryManagement.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ??
                   throw new DirectoryNotFoundException(
                       "Could not locate repository root.");
        }
    }
}
