using FluentAssertions;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InventoryManagement.Tests.IntegrationTests.StockMovements
{
    public class StockMovementMigrationTests
    {
        [Fact]
        public async Task Migration_Should_Create_Opening_Movement_For_Each_Nonzero_Product()
        {
            var databasePath = Path.Combine(
                Path.GetTempPath(),
                $"inventory-ledger-migration-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            try
            {
                await using var db = new ApplicationDbContext(options);
                var migrator = db.Database.GetService<IMigrator>();
                await migrator.MigrateAsync("20260630044355_AddProductUnitsAndDecimalInventory");

                await db.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO Categories (Name, Description, IsActive, CreatedAt)
                    VALUES ('Migration category', 'Test', 1, '2026-06-30 00:00:00');

                    INSERT INTO Products
                        (Name, SKU, Quantity, BaseUnit, DefaultSellingPrice, AverageCost, CategoryId)
                    VALUES
                        ('Nonzero', 'MIG-1', 7.5, 4, 12, 8.25, 1),
                        ('Zero', 'MIG-2', 0, 4, 12, 3.50, 1);
                    """);

                await migrator.MigrateAsync("20260630051810_AddStockMovementLedger");

                var movements = await db.StockMovements.AsNoTracking().ToListAsync();

                movements.Should().ContainSingle();
                movements[0].MovementType.Should().Be(StockMovementType.OpeningStock);
                movements[0].QuantityChange.Should().Be(7.5m);
                movements[0].BalanceBefore.Should().Be(0);
                movements[0].BalanceAfter.Should().Be(7.5m);
                movements[0].UnitCost.Should().Be(8.25m);
                movements[0].SourceType.Should().Be("Migration");
                movements[0].CreatedBy.Should().Be("migration");
            }
            finally
            {
                File.Delete(databasePath);
            }
        }
    }
}
