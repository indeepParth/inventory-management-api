using FluentAssertions;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InventoryManagement.Tests.IntegrationTests.Products;

public class ProductBaseUnitMigrationTests
{
    [Fact]
    public async Task Migration_Should_Map_BaseUnit_Enum_Value_To_Unit_Reference_And_Preserve_Quantity()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"inventory-product-unit-migration-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260731061840_AddUnits");

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Categories (Name, Description, IsActive, CreatedAt)
                VALUES ('Migration category', 'Test', 1, '2026-07-31 00:00:00');

                INSERT INTO Products
                    (Name, SKU, Quantity, BaseUnit, DefaultSellingPrice, AverageCost, CategoryId)
                VALUES
                    ('Migration product', 'MIG-UNIT-1', 7.500, 3, 12.50, 8.25, 1);
                """);

            await migrator.MigrateAsync("20260731063905_ReplaceProductBaseUnitWithUnitReference");

            var product = await db.Products
                .AsNoTracking()
                .Include(x => x.BaseUnit)
                .SingleAsync();

            product.Quantity.Should().Be(7.500m);
            product.DefaultSellingPrice.Should().Be(12.50m);
            product.AverageCost.Should().Be(8.25m);
            product.BaseUnitId.Should().Be(3);
            product.BaseUnit.Name.Should().Be("Bag");
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}
