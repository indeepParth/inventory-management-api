using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Purchases;
using InventoryManagement.Application.Features.Purchases.CreatePurchase;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
