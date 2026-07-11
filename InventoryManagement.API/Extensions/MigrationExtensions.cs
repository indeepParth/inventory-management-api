using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.API.Configuration;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.API.Extensions
{
    public static class MigrationExtensions
    {
        public const string DeploymentMigrationCommand =
            "dotnet ef database update --project InventoryManagement.Infrastructure --startup-project InventoryManagement.API";

        public static async Task<WebApplication> ApplyConfiguredMigrationsAsync(
            this WebApplication app)
        {
            if (app.Environment.IsEnvironment("Testing"))
            {
                return app;
            }

            if (ShouldApplyMigrationsOnStartup(
                    app.Configuration,
                    app.Environment))
            {
                return await app.ApplyMigrationsAsync();
            }

            if (app.Environment.IsProduction())
            {
                await app.ReportPendingMigrationsAsync();
            }

            return app;
        }

        public static bool ShouldApplyMigrationsOnStartup(
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            if (environment.IsEnvironment("Testing"))
            {
                return false;
            }

            var configuredValue = configuration
                .GetValue<bool?>(
                    DatabaseStartupOptions.ApplyMigrationsOnStartupName);

            if (configuredValue.HasValue)
            {
                return configuredValue.Value;
            }

            return environment.IsDevelopment();
        }

        public static async Task<WebApplication> ApplyMigrationsAsync(
        this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            await db.Database.MigrateAsync();

            return app;
        }

        public static async Task<WebApplication> ReportPendingMigrationsAsync(
            this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILogger<ApplicationDbContext>>();

            var pendingMigrations = (await db.Database
                    .GetPendingMigrationsAsync())
                .ToArray();

            if (pendingMigrations.Length == 0)
            {
                logger.LogInformation(
                    "No pending EF Core migrations were found.");

                return app;
            }

            logger.LogWarning(
                "Database has {PendingMigrationCount} pending EF Core migrations. " +
                "Automatic startup migrations are disabled. " +
                "Apply migrations before deployment with: {MigrationCommand}. " +
                "Pending migrations: {PendingMigrations}",
                pendingMigrations.Length,
                DeploymentMigrationCommand,
                string.Join(", ", pendingMigrations));

            throw new InvalidOperationException(
                "Database has pending EF Core migrations, but automatic startup " +
                "migrations are disabled. Apply migrations before deployment with: " +
                DeploymentMigrationCommand);
        }
    }
}
