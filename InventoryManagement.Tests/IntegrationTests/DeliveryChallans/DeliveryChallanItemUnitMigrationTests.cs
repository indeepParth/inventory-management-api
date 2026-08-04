using FluentAssertions;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InventoryManagement.Tests.IntegrationTests.DeliveryChallans;

public class DeliveryChallanItemUnitMigrationTests
{
    [Fact]
    public async Task Migration_Should_Map_Old_Quantity_To_Entered_And_Base_Quantity()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"inventory-challan-unit-migration-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260731070352_AddProductUnitConversions");

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Customers
                    (Name, CreditLimit, BalanceDue, IsActive, CreatedAtUtc, UpdatedAtUtc)
                VALUES
                    ('Migration customer', 0, 0, 1, '2026-07-31 00:00:00', '2026-07-31 00:00:00');

                INSERT INTO Categories
                    (Name, Description, IsActive, CreatedAt)
                VALUES
                    ('Migration category', 'Test', 1, '2026-07-31 00:00:00');

                INSERT INTO Products
                    (Name, SKU, Quantity, BaseUnitId, DefaultSellingPrice, AverageCost, CategoryId)
                VALUES
                    ('Migration product', 'MIG-CH-UNIT-1', 9.500, 3, 12.50, 8.25, 1);

                INSERT INTO DeliveryChallans
                    (ChallanNumber, CustomerId, ChallanDate, Status, DeliveryFromAddress,
                     DeliveryAddress, DeliveryCharge, IsDeliveryChargePaid, CreatedAtUtc,
                     UpdatedAtUtc, CreatedBy)
                VALUES
                    ('MIG-CH-1', 1, '2026-07-31 00:00:00', 0, 'Warehouse',
                     'Customer site', 0, 0, '2026-07-31 00:00:00',
                     '2026-07-31 00:00:00', 'test');

                INSERT INTO DeliveryChallanItems
                    (DeliveryChallanId, ProductId, Quantity)
                VALUES
                    (1, 1, 2.750);
                """);

            await migrator.MigrateAsync("20260731072004_AddDeliveryChallanItemUnits");

            var item = await db.DeliveryChallanItems
                .AsNoTracking()
                .Include(x => x.Unit)
                .SingleAsync();
            var product = await db.Products.AsNoTracking().SingleAsync();

            item.EnteredQuantity.Should().Be(2.750m);
            item.ConvertedBaseQuantity.Should().Be(2.750m);
            item.UnitId.Should().Be(3);
            item.Unit.Name.Should().Be("Bag");
            product.Quantity.Should().Be(9.500m);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}
