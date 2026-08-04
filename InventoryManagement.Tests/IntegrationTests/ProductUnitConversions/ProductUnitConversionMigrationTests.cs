using FluentAssertions;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InventoryManagement.Tests.IntegrationTests.ProductUnitConversions;

public class ProductUnitConversionMigrationTests
{
    [Fact]
    public async Task Migration_Should_Backfill_BaseConversion_And_Preserve_ProductQuantity()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"inventory-product-conversion-migration-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260731063905_ReplaceProductBaseUnitWithUnitReference");

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Categories (Name, Description, IsActive, CreatedAt)
                VALUES ('Migration category', 'Test', 1, '2026-07-31 00:00:00');

                INSERT INTO Products
                    (Name, SKU, Quantity, BaseUnitId, DefaultSellingPrice, AverageCost, CategoryId)
                VALUES
                    ('Migration product', 'MIG-CONV-1', 7.500, 1, 12.50, 8.25, 1);
                """);

            await migrator.MigrateAsync("20260731070352_AddProductUnitConversions");

            var product = await db.Products.AsNoTracking().SingleAsync();
            product.Quantity.Should().Be(7.500m);

            var conversion = await db.ProductUnitConversions
                .AsNoTracking()
                .SingleAsync();
            conversion.ProductId.Should().Be(product.Id);
            conversion.UnitId.Should().Be(1);
            conversion.FactorToBaseUnit.Should().Be(1m);
            conversion.IsActive.Should().BeTrue();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}
