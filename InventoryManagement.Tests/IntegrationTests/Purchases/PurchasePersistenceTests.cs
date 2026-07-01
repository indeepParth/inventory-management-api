using FluentAssertions;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Tests.IntegrationTests.Purchases;

public class PurchasePersistenceTests
{
    [Fact]
    public async Task Database_Should_Enforce_Purchase_Uniqueness_And_Restrictive_Relationships()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var category = new Category
        {
            Name = "Purchase test category",
            Description = "Test",
            IsActive = true
        };
        var supplier = new Supplier { Name = "Purchase test supplier", IsActive = true };
        var product = new Product
        {
            Name = "Purchase test product",
            SKU = "PUR-TEST-1",
            Category = category
        };
        db.AddRange(supplier, product);
        await db.SaveChangesAsync();

        db.Purchases.Add(CreatePurchase("PUR-001", "BILL-001", supplier.Id, product.Id));
        await db.SaveChangesAsync();

        db.Purchases.Add(CreatePurchase("PUR-001", "BILL-002", supplier.Id, product.Id));
        var duplicatePurchaseNumber = () => db.SaveChangesAsync();
        await duplicatePurchaseNumber.Should().ThrowAsync<DbUpdateException>();

        db.ChangeTracker.Clear();
        db.Purchases.Add(CreatePurchase("PUR-002", "BILL-001", supplier.Id, product.Id));
        var duplicateBill = () => db.SaveChangesAsync();
        await duplicateBill.Should().ThrowAsync<DbUpdateException>();

        db.ChangeTracker.Clear();
        supplier = await db.Suppliers.SingleAsync();
        db.Suppliers.Remove(supplier);
        var deleteSupplier = () => db.SaveChangesAsync();
        await deleteSupplier.Should().ThrowAsync<DbUpdateException>();

        db.ChangeTracker.Clear();
        product = await db.Products.SingleAsync();
        db.Products.Remove(product);
        var deleteProduct = () => db.SaveChangesAsync();
        await deleteProduct.Should().ThrowAsync<DbUpdateException>();
    }

    private static Purchase CreatePurchase(
        string purchaseNumber,
        string supplierBillNumber,
        int supplierId,
        int productId)
    {
        return new Purchase
        {
            PurchaseNumber = purchaseNumber,
            SupplierId = supplierId,
            SupplierBillNumber = supplierBillNumber,
            BillDate = new DateTime(2026, 7, 1),
            Status = PurchaseStatus.Draft,
            Subtotal = 100,
            TaxAmount = 18,
            GrandTotal = 118,
            CreatedAtUtc = new DateTime(2026, 7, 1),
            CreatedBy = "test",
            Items =
            {
                new PurchaseItem
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitCost = 100,
                    TaxRate = 18,
                    TaxAmount = 18,
                    LineTotal = 118
                }
            }
        };
    }
}
