using FluentAssertions;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Tests.IntegrationTests.Purchases;

public class PurchasePersistenceTests
{
    [Fact]
    public async Task Database_Should_Enforce_Purchase_Uniqueness_And_Restrictive_Relationships()
    {
        await using var database = new PostgresTestDatabase();
        await database.CreateAsync();

        var options = database.CreateOptions<ApplicationDbContext>();
        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();

        var company = new Company
        {
            Name = $"Persistence company {Guid.NewGuid():N}",
            CreatedAtUtc = DateTime.UtcNow
        };
        var unit = new Unit
        {
            Company = company,
            Name = $"Persistence unit {Guid.NewGuid():N}",
            FactorToBaseUnit = 1,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync();
        unit.BaseUnitId = unit.Id;
        await db.SaveChangesAsync();

        var category = new Category
        {
            CompanyId = company.Id,
            Name = "Purchase test category",
            Description = "Test",
            IsActive = true
        };
        var supplier = new Supplier
        {
            CompanyId = company.Id,
            Name = "Purchase test supplier",
            IsActive = true
        };
        var product = new Product
        {
            CompanyId = company.Id,
            Name = "Purchase test product",
            SKU = "PUR-TEST-1",
            BaseUnitId = unit.Id,
            Category = category
        };
        db.AddRange(supplier, product);
        await db.SaveChangesAsync();

        db.Purchases.Add(CreatePurchase(company.Id, "PUR-001", "BILL-001", supplier.Id, product.Id));
        await db.SaveChangesAsync();

        db.Purchases.Add(CreatePurchase(company.Id, "PUR-001", "BILL-002", supplier.Id, product.Id));
        var duplicatePurchaseNumber = () => db.SaveChangesAsync();
        await duplicatePurchaseNumber.Should().ThrowAsync<DbUpdateException>();

        db.ChangeTracker.Clear();
        db.Purchases.Add(CreatePurchase(company.Id, "PUR-002", "BILL-001", supplier.Id, product.Id));
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
        int companyId,
        string purchaseNumber,
        string supplierBillNumber,
        int supplierId,
        int productId)
    {
        return new Purchase
        {
            CompanyId = companyId,
            PurchaseNumber = purchaseNumber,
            SupplierId = supplierId,
            SupplierBillNumber = supplierBillNumber,
            BillDate = new DateTime(2026, 7, 1),
            Status = PurchaseStatus.Draft,
            Subtotal = 100,
            TaxAmount = 18,
            GrandTotal = 118,
            CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
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
