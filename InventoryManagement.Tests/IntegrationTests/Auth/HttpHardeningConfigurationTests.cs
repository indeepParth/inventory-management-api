using System.Net;
using FluentAssertions;
using InventoryManagement.API.Configuration;
using InventoryManagement.Application.Common.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class HttpHardeningConfigurationTests
    {
        [Fact]
        public void Validate_Should_Succeed_With_Trusted_Production_Proxy()
        {
            var configuration = BuildConfiguration(ValidProductionSettings());

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_Should_Fail_When_Production_Trusted_Forwarder_Is_Missing()
        {
            var settings = ValidProductionSettings();
            settings.Remove("ReverseProxy:KnownProxies:0");
            var configuration = BuildConfiguration(settings);

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*ReverseProxy:KnownProxies or ReverseProxy:KnownNetworks must contain at least one trusted proxy or network*");
        }

        [Theory]
        [InlineData("ReverseProxy:KnownProxies:0", "not-an-ip", "ReverseProxy:KnownProxies contains an invalid IP address")]
        [InlineData("ReverseProxy:KnownNetworks:0", "10.0.0.0/not-a-prefix", "ReverseProxy:KnownNetworks contains an invalid CIDR network")]
        public void Validate_Should_Fail_When_Production_Trusted_Forwarder_Is_Invalid(
            string settingName,
            string settingValue,
            string expectedMessage)
        {
            var settings = ValidProductionSettings();
            settings.Remove("ReverseProxy:KnownProxies:0");
            settings[settingName] = settingValue;
            var configuration = BuildConfiguration(settings);

            var act = () => StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production));

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage($"*{expectedMessage}*");
        }

        [Fact]
        public void Configure_Should_Use_Only_Configured_Trusted_Forwarders()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["ReverseProxy:KnownProxies:0"] = "10.0.0.10",
                ["ReverseProxy:KnownNetworks:0"] = "10.0.1.0/24"
            });
            var options = new ForwardedHeadersOptions();

            ForwardedHeadersConfiguration.Configure(options, configuration);

            options.ForwardedHeaders.Should().Be(
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto);
            options.ForwardLimit.Should().Be(1);
            options.KnownProxies.Should().Contain(IPAddress.Parse("10.0.0.10"));
            options.KnownProxies.Should().NotContain(IPAddress.Any);
            options.KnownNetworks.Should().NotContain(network =>
                network.Prefix.Equals(IPAddress.Any) &&
                network.PrefixLength == 0);
        }

        [Fact]
        public void Swagger_Should_Be_Disabled_In_Production_By_Default()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var enabled = SwaggerConfiguration.IsEnabled(
                configuration,
                new TestHostEnvironment(Environments.Production));

            enabled.Should().BeFalse();
        }

        [Fact]
        public void Swagger_Should_Be_Enabled_In_Production_When_Explicitly_Configured()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Swagger:Enabled"] = "true"
            });

            var enabled = SwaggerConfiguration.IsEnabled(
                configuration,
                new TestHostEnvironment(Environments.Production));

            enabled.Should().BeTrue();
        }

        [Fact]
        public void Swagger_Should_Remain_Enabled_In_Development()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var enabled = SwaggerConfiguration.IsEnabled(
                configuration,
                new TestHostEnvironment(Environments.Development));

            enabled.Should().BeTrue();
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
