using FluentAssertions;
using InventoryManagement.API.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class MigrationStartupTests
    {
        [Fact]
        public void ShouldApplyMigrationsOnStartup_Should_Default_To_True_In_Development()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var result = MigrationExtensions.ShouldApplyMigrationsOnStartup(
                configuration,
                new TestHostEnvironment(Environments.Development));

            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldApplyMigrationsOnStartup_Should_Default_To_False_In_Production()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var result = MigrationExtensions.ShouldApplyMigrationsOnStartup(
                configuration,
                new TestHostEnvironment(Environments.Production));

            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldApplyMigrationsOnStartup_Should_Allow_Production_Opt_In()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = "true"
            });

            var result = MigrationExtensions.ShouldApplyMigrationsOnStartup(
                configuration,
                new TestHostEnvironment(Environments.Production));

            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldApplyMigrationsOnStartup_Should_Allow_Development_Opt_Out()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = "false"
            });

            var result = MigrationExtensions.ShouldApplyMigrationsOnStartup(
                configuration,
                new TestHostEnvironment(Environments.Development));

            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldApplyMigrationsOnStartup_Should_Preserve_Testing_Skip()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = "true"
            });

            var result = MigrationExtensions.ShouldApplyMigrationsOnStartup(
                configuration,
                new TestHostEnvironment("Testing"));

            result.Should().BeFalse();
        }

        [Fact]
        public void DeploymentMigrationCommand_Should_Be_Documented_Command()
        {
            MigrationExtensions.DeploymentMigrationCommand.Should().Be(
                "dotnet ef database update --project InventoryManagement.Infrastructure --startup-project InventoryManagement.API");
        }

        private static IConfiguration BuildConfiguration(
            Dictionary<string, string?> settings)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
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
