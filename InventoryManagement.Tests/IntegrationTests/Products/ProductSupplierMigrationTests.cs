using FluentAssertions;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InventoryManagement.Tests.IntegrationTests.Products;

public class ProductSupplierMigrationTests
{
    [Fact]
    public async Task Migration_Should_Remove_Relationship_And_Preserve_Product_And_Supplier()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"inventory-product-supplier-migration-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260630051810_AddStockMovementLedger");

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Categories (Name, Description, IsActive, CreatedAt)
                VALUES ('Migration category', 'Test', 1, '2026-07-01 00:00:00');

                INSERT INTO Suppliers (Name, IsActive, CreatedAt)
                VALUES ('Migration supplier', 1, '2026-07-01 00:00:00');

                INSERT INTO Products
                    (Name, SKU, Quantity, BaseUnit, DefaultSellingPrice, AverageCost, CategoryId, SupplierId)
                VALUES
                    ('Migration product', 'MIG-SUP-1', 0, 0, 12, 0, 1, 1);
                """);

            await migrator.MigrateAsync("20260701051812_RemoveProductSupplierRelationship");

            (await db.Products.AsNoTracking().SingleAsync()).Name
                .Should().Be("Migration product");
            (await db.Suppliers.AsNoTracking().SingleAsync()).Name
                .Should().Be("Migration supplier");
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}
