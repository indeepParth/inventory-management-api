using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.DeliveryChallans;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.DeliveryChallans
{
    public class DeliveryChallanEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public DeliveryChallanEndpointsTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_And_Post_Should_Store_Entered_Unit_And_Post_Base_Quantity()
        {
            await AuthenticateAsync();
            var seed = await SeedProductWithConversionAsync();

            var createResponse = await Client.PostAsJsonAsync(
                "/api/delivery-challans",
                new
                {
                    CustomerId = seed.CustomerId,
                    ChallanDate = new DateTime(2026, 7, 31),
                    DeliveryFromAddress = "Warehouse",
                    DeliveryAddress = "Customer site",
                    Items = new[]
                    {
                        new
                        {
                            ProductId = seed.ProductId,
                            EnteredQuantity = 1.5m,
                            UnitId = 3
                        }
                    }
                });

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var draft = await createResponse.Content
                .ReadFromJsonAsync<DeliveryChallanResponse>();
            draft.Should().NotBeNull();
            draft!.Items.Should().ContainSingle(x =>
                x.EnteredQuantity == 1.5m &&
                x.UnitId == 3 &&
                x.UnitName == "Bag" &&
                x.ConvertedBaseQuantity == 3m &&
                x.Quantity == 3m);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var product = await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId);
                product.Quantity.Should().Be(10m);
            }

            var postResponse = await Client.PostAsync(
                $"/api/delivery-challans/{draft.Id}/post",
                null);

            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var product = await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId);
                product.Quantity.Should().Be(7m);
                var movement = await db.StockMovements.AsNoTracking()
                    .SingleAsync(x => x.ProductId == seed.ProductId);
                movement.QuantityChange.Should().Be(-3m);
            }
        }

        [Fact]
        public async Task Create_Should_Reject_Unit_Without_Active_Product_Conversion()
        {
            await AuthenticateAsync();
            var seed = await SeedProductWithConversionAsync();

            var response = await Client.PostAsJsonAsync(
                "/api/delivery-challans",
                new
                {
                    CustomerId = seed.CustomerId,
                    ChallanDate = new DateTime(2026, 7, 31),
                    DeliveryFromAddress = "Warehouse",
                    DeliveryAddress = "Customer site",
                    Items = new[]
                    {
                        new
                        {
                            ProductId = seed.ProductId,
                            EnteredQuantity = 1m,
                            UnitId = 2
                        }
                    }
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData(DeliveryChallanStatus.Posted)]
        [InlineData(DeliveryChallanStatus.Invoiced)]
        public async Task MarkDeliveryChargePaid_Should_Persist_For_Allowed_Statuses(
            DeliveryChallanStatus status)
        {
            await AuthenticateAsync();
            var challanId = await SeedChallanAsync(status, deliveryCharge: 80);

            var response = await Client.PostAsync(
                $"/api/delivery-challans/{challanId}/delivery-charge/mark-paid",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content
                .ReadFromJsonAsync<DeliveryChallanResponse>();
            result.Should().NotBeNull();
            result!.IsDeliveryChargePaid.Should().BeTrue();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var challan = await db.DeliveryChallans.AsNoTracking()
                .SingleAsync(x => x.Id == challanId);
            challan.IsDeliveryChargePaid.Should().BeTrue();
            (await db.Payments.CountAsync()).Should().Be(0);
        }

        [Theory]
        [InlineData(DeliveryChallanStatus.Draft, 80)]
        [InlineData(DeliveryChallanStatus.Cancelled, 80)]
        [InlineData(DeliveryChallanStatus.Posted, 0)]
        public async Task MarkDeliveryChargePaid_Should_Reject_Blocked_Challans(
            DeliveryChallanStatus status,
            decimal deliveryCharge)
        {
            await AuthenticateAsync();
            var challanId = await SeedChallanAsync(status, deliveryCharge);

            var response = await Client.PostAsync(
                $"/api/delivery-challans/{challanId}/delivery-charge/mark-paid",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var challan = await db.DeliveryChallans.AsNoTracking()
                .SingleAsync(x => x.Id == challanId);
            challan.IsDeliveryChargePaid.Should().BeFalse();
            (await db.Payments.CountAsync()).Should().Be(0);
        }

        private async Task<int> SeedChallanAsync(
            DeliveryChallanStatus status,
            decimal deliveryCharge)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var customer = new Customer
            {
                Name = $"Challan customer {suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var product = new Product
            {
                Name = $"Challan product {suffix}",
                SKU = $"CH-{suffix}",
                Quantity = 10,
                BaseUnitId = 1,
                AverageCost = 20,
                Category = new Category
                {
                    Name = $"Challan category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            var challan = new DeliveryChallan
            {
                ChallanNumber = $"DC-{suffix}",
                Customer = customer,
                ChallanDate = new DateTime(2026, 7, 1),
                Status = status,
                DeliveryFromAddress = "Warehouse",
                DeliveryAddress = "Customer site",
                DeliveryCharge = deliveryCharge,
                IsDeliveryChargePaid = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedBy = "test",
                Items =
                {
                    new DeliveryChallanItem
                    {
                        Product = product,
                        EnteredQuantity = 1,
                        UnitId = 1,
                        ConvertedBaseQuantity = 1
                    }
                }
            };

            db.DeliveryChallans.Add(challan);
            await db.SaveChangesAsync();
            return challan.Id;
        }

        private async Task<ConversionSeed> SeedProductWithConversionAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var customer = new Customer
            {
                Name = $"Conversion customer {suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var product = new Product
            {
                Name = $"Conversion product {suffix}",
                SKU = $"CH-CONV-{suffix}",
                Quantity = 10,
                BaseUnitId = 1,
                AverageCost = 20,
                Category = new Category
                {
                    Name = $"Conversion category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                UnitConversions =
                {
                    new ProductUnitConversion
                    {
                        UnitId = 1,
                        FactorToBaseUnit = 1,
                        IsActive = true
                    },
                    new ProductUnitConversion
                    {
                        UnitId = 3,
                        FactorToBaseUnit = 2,
                        IsActive = true
                    }
                }
            };
            db.AddRange(customer, product);
            await db.SaveChangesAsync();

            return new ConversionSeed(customer.Id, product.Id);
        }

        private sealed record ConversionSeed(int CustomerId, int ProductId);
    }
}
