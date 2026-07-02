using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Purchases;
using InventoryManagement.Application.Features.SupplierReturns;
using InventoryManagement.Application.Features.SupplierReturns.CreateSupplierReturn;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CreatePurchaseCommand =
    InventoryManagement.Application.Features.Purchases.CreatePurchase.Command;
using CreatePurchaseItemInput =
    InventoryManagement.Application.Features.Purchases.CreatePurchase.PurchaseItemInput;

namespace InventoryManagement.Tests.IntegrationTests.SupplierReturns
{
    public class SupplierReturnEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public SupplierReturnEndpointsTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Post_And_Cancel_Should_Reverse_Stock_Payable_And_Value()
        {
            await AuthenticateAsync();
            var seed = await SeedAsync();
            var purchase = await CreateAndPostPurchaseAsync(seed);
            var supplierReturn = await CreateReturnAsync(purchase, 2);

            supplierReturn.Status.Should().Be(SupplierReturnStatus.Draft);
            supplierReturn.Subtotal.Should().Be(80);
            supplierReturn.TaxAmount.Should().Be(14.40m);
            supplierReturn.GrandTotal.Should().Be(94.40m);
            supplierReturn.Items[0].UnitCost.Should().Be(40);
            supplierReturn.Items[0].TaxRate.Should().Be(18);

            var post = await Client.PostAsync(
                $"/api/supplier-returns/{supplierReturn.Id}/post",
                null);
            post.StatusCode.Should().Be(HttpStatusCode.OK);
            var repeatedPost = await Client.PostAsync(
                $"/api/supplier-returns/{supplierReturn.Id}/post",
                null);
            repeatedPost.StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var product = await db.Products
                    .SingleAsync(x => x.Id == seed.ProductId);
                product.Quantity.Should().Be(13);
                product.AverageCost.Should().Be(24.62m);
                (await db.Purchases.SingleAsync(x => x.Id == purchase.Id))
                    .BalanceDue.Should().Be(141.60m);
                var movement = await db.StockMovements.SingleAsync(x =>
                    x.SourceType == "SupplierReturn" &&
                    x.SourceId == supplierReturn.Id.ToString());
                movement.MovementType.Should()
                    .Be(StockMovementType.SupplierReturn);
                movement.QuantityChange.Should().Be(-2);
                movement.UnitCost.Should().Be(40);
            }

            var cancel = await Client.PostAsync(
                $"/api/supplier-returns/{supplierReturn.Id}/cancel",
                null);
            cancel.StatusCode.Should().Be(HttpStatusCode.OK);
            var cancelled = await cancel.Content
                .ReadFromJsonAsync<SupplierReturnResponse>();
            cancelled!.Status.Should().Be(SupplierReturnStatus.Cancelled);
            var repeatedCancel = await Client.PostAsync(
                $"/api/supplier-returns/{supplierReturn.Id}/cancel",
                null);
            repeatedCancel.StatusCode.Should().Be(HttpStatusCode.OK);

            using var finalScope = _factory.Services.CreateScope();
            var finalDb = finalScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var finalProduct = await finalDb.Products
                .SingleAsync(x => x.Id == seed.ProductId);
            finalProduct.Quantity.Should().Be(15);
            finalProduct.AverageCost.Should().Be(26.67m);
            (await finalDb.Purchases.SingleAsync(x => x.Id == purchase.Id))
                .BalanceDue.Should().Be(236);
            (await finalDb.StockMovements.SingleAsync(x =>
                x.SourceType == "SupplierReturnCancellation" &&
                x.SourceId == supplierReturn.Id.ToString()))
                .QuantityChange.Should().Be(2);
            (await finalDb.StockMovements.CountAsync(x =>
                x.SourceId == supplierReturn.Id.ToString() &&
                (x.SourceType == "SupplierReturn" ||
                 x.SourceType == "SupplierReturnCancellation")))
                .Should().Be(2);
        }

        [Fact]
        public async Task Post_Should_Reject_Quantity_Already_Returned()
        {
            await AuthenticateAsync();
            var seed = await SeedAsync();
            var purchase = await CreateAndPostPurchaseAsync(seed);
            var first = await CreateReturnAsync(purchase, 3);
            var second = await CreateReturnAsync(purchase, 3);

            (await Client.PostAsync(
                $"/api/supplier-returns/{first.Id}/post",
                null)).EnsureSuccessStatusCode();
            var rejected = await Client.PostAsync(
                $"/api/supplier-returns/{second.Id}/post",
                null);

            rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await rejected.Content.ReadAsStringAsync();
            body.Should().Contain("remaining returnable quantity");
            body.Should().Contain("traceId");
        }

        [Fact]
        public async Task Post_Should_Reject_Insufficient_Available_Stock()
        {
            await AuthenticateAsync();
            var seed = await SeedAsync();
            var purchase = await CreateAndPostPurchaseAsync(seed);
            var supplierReturn = await CreateReturnAsync(purchase, 2);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var product = await db.Products
                    .SingleAsync(x => x.Id == seed.ProductId);
                product.Quantity = 1;
                await db.SaveChangesAsync();
            }

            var response = await Client.PostAsync(
                $"/api/supplier-returns/{supplierReturn.Id}/post",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync())
                .Should().Contain("Insufficient stock");
        }

        [Fact]
        public async Task Create_Should_Reject_NonPosted_Purchase()
        {
            await AuthenticateAsync();
            var seed = await SeedAsync();
            var purchase = await CreatePurchaseAsync(seed);

            var response = await Client.PostAsJsonAsync(
                "/api/supplier-returns",
                new Command
                {
                    ReturnNumber = $"SRET-{Guid.NewGuid():N}",
                    PurchaseId = purchase.Id,
                    ReturnDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new SupplierReturnItemInput
                        {
                            PurchaseItemId = purchase.Items[0].Id,
                            Quantity = 1
                        }
                    }
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("posted purchases");
            body.Should().Contain("traceId");
        }

        private async Task<SupplierReturnResponse> CreateReturnAsync(
            PurchaseResponse purchase,
            decimal quantity)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/supplier-returns",
                new Command
                {
                    ReturnNumber = $"SRET-{Guid.NewGuid():N}",
                    PurchaseId = purchase.Id,
                    ReturnDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new SupplierReturnItemInput
                        {
                            PurchaseItemId = purchase.Items[0].Id,
                            Quantity = quantity
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<SupplierReturnResponse>())!;
        }

        private async Task<PurchaseResponse> CreateAndPostPurchaseAsync(
            SeedResult seed)
        {
            var purchase = await CreatePurchaseAsync(seed);
            var response = await Client.PostAsync(
                $"/api/purchases/{purchase.Id}/post",
                null);
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<PurchaseResponse>())!;
        }

        private async Task<PurchaseResponse> CreatePurchaseAsync(SeedResult seed)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/purchases",
                new CreatePurchaseCommand
                {
                    PurchaseNumber = $"PUR-{Guid.NewGuid():N}",
                    SupplierId = seed.SupplierId,
                    BillDate = new DateTime(2026, 7, 1),
                    Items =
                    {
                        new CreatePurchaseItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 5,
                            UnitCost = 40,
                            TaxRate = 18
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<PurchaseResponse>())!;
        }

        private async Task<SeedResult> SeedAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var supplier = new Supplier
            {
                Name = $"Return supplier {suffix}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var product = new Product
            {
                Name = $"Supplier return product {suffix}",
                SKU = $"SRET-{suffix}",
                Quantity = 10,
                AverageCost = 20,
                Category = new Category
                {
                    Name = $"Supplier return category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            db.AddRange(supplier, product);
            await db.SaveChangesAsync();
            return new SeedResult(supplier.Id, product.Id);
        }

        private sealed record SeedResult(int SupplierId, int ProductId);
    }
}
