using FluentAssertions;
using InventoryManagement.API.Configuration;
using InventoryManagement.Application.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class CorsConfigurationTests
    {
        [Fact]
        public void Validate_Should_Accept_Multiple_Production_Https_Origins()
        {
            var settings = ValidProductionSettings();
            settings["Cors:AllowedOrigins:1"] = "https://admin.inventory.example.com";
            var configuration = BuildConfiguration(settings);

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("http://localhost:5173")]
        [InlineData("https://inventory.example.com/app")]
        [InlineData("https://inventory.example.com?tenant=1")]
        public void Validate_Should_Reject_Non_Production_Safe_Origins(
            string origin)
        {
            var settings = ValidProductionSettings();
            settings["Cors:AllowedOrigins:0"] = origin;
            var configuration = BuildConfiguration(settings);

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*Cors:AllowedOrigins must contain only absolute HTTPS origins in Production*");
        }

        [Fact]
        public void Validate_Should_Require_At_Least_One_Production_Origin()
        {
            var settings = ValidProductionSettings();
            settings.Remove("Cors:AllowedOrigins:0");
            var configuration = BuildConfiguration(settings);

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*Cors:AllowedOrigins must contain at least one origin*");
        }

        private static IConfiguration BuildConfiguration(
            Dictionary<string, string?> settings)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private static Dictionary<string, string?> ValidProductionSettings()
        {
            return new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=Data/inventory.db",
                ["Jwt:Issuer"] = "InventoryManagement",
                ["Jwt:Audience"] = "InventoryManagementUsers",
                ["Jwt:Key"] = new string('P', JwtOptions.MinimumSigningKeyBytes),
                ["Cors:AllowedOrigins:0"] = "https://inventory.example.com",
                ["ReverseProxy:KnownProxies:0"] = "10.0.0.10"
            };
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public TestHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; } = "InventoryManagement.Tests";
            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }
    }
}
