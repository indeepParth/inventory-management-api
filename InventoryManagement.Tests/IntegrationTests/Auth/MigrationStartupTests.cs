using FluentAssertions;
using InventoryManagement.API.Extensions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

        [Fact]
        public async Task Fresh_Database_Migrations_Should_Create_Multi_Company_Schema()
        {
            await using var database = new PostgresTestDatabase();
            await database.CreateAsync();
            await using var context = new ApplicationDbContext(
                database.CreateOptions<ApplicationDbContext>());

            await context.Database.MigrateAsync();

            var pendingMigrations = await context.Database
                .GetPendingMigrationsAsync();
            pendingMigrations.Should().BeEmpty();
            context.Companies.Should().BeEmpty();
            context.Units.Should().BeEmpty();

            var requiredCompanyTables = new[]
            {
                "Categories",
                "CompanyProfiles",
                "CustomerReturns",
                "Customers",
                "DeliveryChallans",
                "DocumentSequences",
                "Drivers",
                "Payments",
                "Products",
                "Purchases",
                "SalesInvoices",
                "StockMovements",
                "SupplierReturns",
                "Suppliers",
                "Units"
            };

            foreach (var table in requiredCompanyTables)
            {
                var exists = await RequiredCompanyIdColumnExistsAsync(
                    database.ConnectionString,
                    table);
                exists.Should().BeTrue($"{table}.CompanyId should be required");
            }

            var indexNames = await GetIndexNamesAsync(database.ConnectionString);
            indexNames.Should().Contain("IX_CompanyUsers_UserId_CompanyId");
            indexNames.Should().Contain("IX_Companies_BillingOwnerUserId");
            indexNames.Should().Contain("IX_DocumentSequences_CompanyId_DocumentType_Year");
            indexNames.Should().Contain("IX_Products_CompanyId_SKU");
            indexNames.Should().Contain("IX_SalesInvoices_CompanyId_InvoiceNumber");
            indexNames.Should().Contain("IX_Purchases_CompanyId_PurchaseNumber");
            indexNames.Should().Contain("IX_Payments_CompanyId_ReceiptNumber");
            indexNames.Should().Contain("IX_SubscriptionPlans_Code");
            indexNames.Should().Contain("IX_UserSubscriptions_UserId");

            var billingOwnerRequired = await RequiredColumnExistsAsync(
                database.ConnectionString,
                "Companies",
                "BillingOwnerUserId");
            billingOwnerRequired.Should().BeTrue(
                "Companies.BillingOwnerUserId should be required");

            var freePlan = await context.SubscriptionPlans
                .SingleOrDefaultAsync(x => x.Code == SubscriptionPlans.Free);
            freePlan.Should().NotBeNull();
            freePlan!.IsActive.Should().BeTrue();
            freePlan.MaxCompanies.Should().Be(1);
            freePlan.MaxUsersPerCompany.Should().Be(3);
            freePlan.MaxInvoicesPerMonth.Should().Be(100);
            freePlan.MaxProducts.Should().Be(500);
            freePlan.MaxCustomers.Should().Be(500);
        }

        private static IConfiguration BuildConfiguration(
            Dictionary<string, string?> settings)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private static async Task<bool> RequiredCompanyIdColumnExistsAsync(
            string connectionString,
            string tableName)
        {
            return await RequiredColumnExistsAsync(
                connectionString,
                tableName,
                "CompanyId");
        }

        private static async Task<bool> RequiredColumnExistsAsync(
            string connectionString,
            string tableName,
            string columnName)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = @tableName
                      AND column_name = @columnName
                      AND is_nullable = 'NO');
                """;
            command.Parameters.AddWithValue("tableName", tableName);
            command.Parameters.AddWithValue("columnName", columnName);

            return (bool)(await command.ExecuteScalarAsync())!;
        }

        private static async Task<List<string>> GetIndexNamesAsync(
            string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT indexname
                FROM pg_indexes
                WHERE schemaname = 'public';
                """;

            var names = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            return names;
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
