using FluentAssertions;
using InventoryManagement.API.Configuration;
using InventoryManagement.Application.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class JwtConfigurationTests
    {
        [Fact]
        public void Validate_Should_Succeed_With_Valid_Production_Configuration()
        {
            var configuration = BuildConfiguration(ValidProductionSettings());

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("ConnectionStrings:DefaultConnection")]
        [InlineData("Jwt:Issuer")]
        [InlineData("Jwt:Audience")]
        [InlineData("Jwt:Key")]
        [InlineData("Cors:AllowedOrigins:0")]
        public void Validate_Should_Fail_When_Production_Required_Setting_Is_Missing(
            string settingName)
        {
            var settings = ValidProductionSettings();
            settings.Remove(settingName);
            var configuration = BuildConfiguration(settings);

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*Production configuration is invalid*")
                .Which.Message.Should().Contain(
                    settingName.Replace(":0", string.Empty));
        }

        [Fact]
        public void Validate_Should_Fail_When_Production_Jwt_Key_Is_Weak()
        {
            var settings = ValidProductionSettings();
            settings["Jwt:Key"] = "short";
            var configuration = BuildConfiguration(settings);

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*Jwt:Key must be at least 32 UTF-8 bytes*");
        }

        [Fact]
        public void Validate_Should_Fail_When_Production_Allowed_Origin_Is_Invalid()
        {
            var settings = ValidProductionSettings();
            settings["Cors:AllowedOrigins:0"] = "localhost:5173";
            var configuration = BuildConfiguration(settings);

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*Cors:AllowedOrigins must contain only absolute HTTPS origins in Production*");
        }

        [Fact]
        public void Validate_Should_Not_Require_Production_Settings_Outside_Production()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Development));

            act.Should().NotThrow();
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
