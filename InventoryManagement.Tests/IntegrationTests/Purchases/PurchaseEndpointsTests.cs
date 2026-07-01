using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Purchases;
using InventoryManagement.Application.Features.Purchases.CreatePurchase;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdatePurchaseCommand = InventoryManagement.Application.Features.Purchases.UpdatePurchase.Command;
using UpdatePurchaseItemInput = InventoryManagement.Application.Features.Purchases.UpdatePurchase.PurchaseItemInput;

namespace InventoryManagement.Tests.IntegrationTests.Purchases
{
    public class PurchaseEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public PurchaseEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_Then_Get_Should_Return_Server_Totals_Without_Changing_Stock()
        {
            await AuthenticateAsync();
            var seed = await SeedPurchaseDependenciesAsync();

            var command = new Command
            {
                PurchaseNumber = $"PUR-{Guid.NewGuid():N}",
                SupplierId = seed.SupplierId,
                SupplierBillNumber = $"BILL-{Guid.NewGuid():N}",
                BillDate = new DateTime(2026, 7, 1),
                Discount = 5,
                OtherCharges = 3,
                Notes = "Draft purchase",
                Items =
                {
                    new PurchaseItemInput
                    {
                        ProductId = seed.ProductId,
                        Quantity = 2.5m,
                        UnitCost = 40,
                        TaxRate = 18
                    }
                }
            };

            var createResponse = await Client.PostAsJsonAsync("/api/purchases", command);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<PurchaseResponse>();
            created.Should().NotBeNull();
            created!.Status.Should().Be(PurchaseStatus.Draft);
            created.SupplierName.Should().Be(seed.SupplierName);
            created.Subtotal.Should().Be(100);
            created.TaxAmount.Should().Be(18);
            created.GrandTotal.Should().Be(116);
            created.Items.Should().ContainSingle();
            created.Items[0].ProductName.Should().Be(seed.ProductName);
            created.Items[0].ProductSku.Should().Be(seed.ProductSku);
            created.Items[0].LineTotal.Should().Be(118);

            var getResponse = await Client.GetAsync($"/api/purchases/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetched = await getResponse.Content.ReadFromJsonAsync<PurchaseResponse>();
            fetched.Should().BeEquivalentTo(created);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = await db.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId);
            product.Quantity.Should().Be(seed.Quantity);
            product.AverageCost.Should().Be(seed.AverageCost);
            (await db.StockMovements.CountAsync(x => x.ProductId == seed.ProductId))
                .Should().Be(seed.StockMovementCount);
        }

        [Fact]
        public async Task Create_Should_Return_Validation_Errors_For_Empty_Items()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync("/api/purchases", new Command
            {
                PurchaseNumber = $"PUR-{Guid.NewGuid():N}",
                SupplierId = 1,
                BillDate = new DateTime(2026, 7, 1),
                Discount = -1,
                Items = new List<PurchaseItemInput>()
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task List_Should_Page_And_Apply_All_Filters()
        {
            await AuthenticateAsync();
            var seed = await SeedPurchaseDependenciesAsync();
            var matchingNumber = $"MATCH-{Guid.NewGuid():N}";
            var matchingBill = $"FILTER-{Guid.NewGuid():N}";

            await CreatePurchaseAsync(
                seed,
                matchingNumber,
                matchingBill,
                new DateTime(2026, 7, 10));
            await CreatePurchaseAsync(
                seed,
                $"OTHER-{Guid.NewGuid():N}",
                $"OTHER-{Guid.NewGuid():N}",
                new DateTime(2026, 6, 1));

            var response = await Client.GetAsync(
                $"/api/purchases?pageNumber=1&pageSize=1" +
                $"&supplierId={seed.SupplierId}&status=Draft" +
                "&dateFrom=2026-07-01&dateTo=2026-07-31" +
                $"&purchaseNumber={matchingNumber[..12]}" +
                $"&supplierBillNumber={matchingBill[..12]}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content
                .ReadFromJsonAsync<PagedResponse<PurchaseResponse>>();
            page.Should().NotBeNull();
            page!.Items.Should().ContainSingle(x =>
                x.PurchaseNumber == matchingNumber &&
                x.SupplierBillNumber == matchingBill);
            page.PageNumber.Should().Be(1);
            page.PageSize.Should().Be(1);
            page.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Update_Draft_Should_Replace_Lines_Without_Changing_Stock()
        {
            await AuthenticateAsync();
            var seed = await SeedPurchaseDependenciesAsync();
            var created = await CreatePurchaseAsync(
                seed,
                $"EDIT-{Guid.NewGuid():N}",
                null,
                new DateTime(2026, 7, 1));

            var update = new UpdatePurchaseCommand(
                0,
                $"EDITED-{Guid.NewGuid():N}",
                seed.SupplierId,
                $"EDIT-BILL-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 2),
                4,
                2,
                "Replaced draft",
                new List<UpdatePurchaseItemInput>
                {
                    new()
                    {
                        ProductId = seed.ProductId,
                        Quantity = 3,
                        UnitCost = 20,
                        TaxRate = 5
                    }
                });

            var response = await Client.PutAsJsonAsync(
                $"/api/purchases/{created.Id}",
                update);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content.ReadFromJsonAsync<PurchaseResponse>();
            updated.Should().NotBeNull();
            updated!.Status.Should().Be(PurchaseStatus.Draft);
            updated.Subtotal.Should().Be(60);
            updated.TaxAmount.Should().Be(3);
            updated.GrandTotal.Should().Be(61);
            updated.Items.Should().ContainSingle();
            updated.Items[0].Quantity.Should().Be(3);
            updated.CreatedAtUtc.Should().Be(created.CreatedAtUtc);
            updated.CreatedBy.Should().Be(created.CreatedBy);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = await db.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId);
            product.Quantity.Should().Be(seed.Quantity);
            product.AverageCost.Should().Be(seed.AverageCost);
            (await db.StockMovements.CountAsync(x => x.ProductId == seed.ProductId))
                .Should().Be(seed.StockMovementCount);
        }

        private async Task<SeedResult> SeedPurchaseDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var supplier = new Supplier
            {
                Name = $"Purchase supplier {suffix}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var product = new Product
            {
                Name = $"Purchase product {suffix}",
                SKU = $"PUR-{suffix}",
                Quantity = 12.5m,
                AverageCost = 32,
                Category = new Category
                {
                    Name = $"Purchase category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            db.AddRange(supplier, product);
            await db.SaveChangesAsync();

            return new SeedResult(
                supplier.Id,
                supplier.Name,
                product.Id,
                product.Name,
                product.SKU,
                product.Quantity,
                product.AverageCost,
                await db.StockMovements.CountAsync(x => x.ProductId == product.Id));
        }

        private async Task<PurchaseResponse> CreatePurchaseAsync(
            SeedResult seed,
            string purchaseNumber,
            string? supplierBillNumber,
            DateTime billDate)
        {
            var response = await Client.PostAsJsonAsync("/api/purchases", new Command
            {
                PurchaseNumber = purchaseNumber,
                SupplierId = seed.SupplierId,
                SupplierBillNumber = supplierBillNumber,
                BillDate = billDate,
                Items =
                {
                    new PurchaseItemInput
                    {
                        ProductId = seed.ProductId,
                        Quantity = 1,
                        UnitCost = 10
                    }
                }
            });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<PurchaseResponse>())!;
        }

        private sealed record SeedResult(
            int SupplierId,
            string SupplierName,
            int ProductId,
            string ProductName,
            string ProductSku,
            decimal Quantity,
            decimal AverageCost,
            int StockMovementCount);
    }
}
