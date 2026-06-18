using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.API.Extensions
{
    public static class MigrationExtensions
    {
        public static async Task<WebApplication> ApplyMigrationsAsync(
        this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            await db.Database.MigrateAsync();

            return app;
        }
    }
}