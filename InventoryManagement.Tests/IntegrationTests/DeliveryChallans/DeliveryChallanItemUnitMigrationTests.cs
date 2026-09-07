using FluentAssertions;
using InventoryManagement.Infrastructure.Persistence;
using System.Data;
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

            var item = await ReadSingleItemAsync(db);
            var productQuantity = await ReadSingleProductQuantityAsync(db);

            item.EnteredQuantity.Should().Be(2.750m);
            item.ConvertedBaseQuantity.Should().Be(2.750m);
            item.UnitId.Should().Be(3);
            item.UnitName.Should().Be("Bag");
            productQuantity.Should().Be(9.500m);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static async Task<ItemSnapshot> ReadSingleItemAsync(
        ApplicationDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT i."EnteredQuantity", i."ConvertedBaseQuantity", i."UnitId", u."Name"
            FROM "DeliveryChallanItems" i
            INNER JOIN "Units" u ON u."Id" = i."UnitId"
            LIMIT 1;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new ItemSnapshot(
            reader.GetDecimal(0),
            reader.GetDecimal(1),
            reader.GetInt32(2),
            reader.GetString(3));
    }

    private static async Task<decimal> ReadSingleProductQuantityAsync(
        ApplicationDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        await using var command = connection.CreateCommand();
        command.CommandText = """SELECT "Quantity" FROM "Products" LIMIT 1;""";
        var value = await command.ExecuteScalarAsync();
        return Convert.ToDecimal(value);
    }

    private sealed record ItemSnapshot(
        decimal EnteredQuantity,
        decimal ConvertedBaseQuantity,
        int UnitId,
        string UnitName);
}
