using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Features.StockMovements.GetStockMovements;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.StockMovements
{
    public class GetStockMovementsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public GetStockMovementsTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Should_Require_Authentication()
        {
            var response = await Client.GetAsync("/api/stock-movements");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Get_Should_Filter_Page_And_Sort_Newest_First()
        {
            await AuthenticateAsync();
            var now = new DateTime(2026, 6, 30, 8, 0, 0, DateTimeKind.Utc);
            int productId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var category = new Category
                {
                    Name = $"Ledger {Guid.NewGuid():N}",
                    Description = "Ledger test",
                    CreatedAt = now
                };
                var product = new Product
                {
                    Name = "Ledger product",
                    SKU = $"LED-{Guid.NewGuid():N}",
                    Category = category,
                    BaseUnit = UnitOfMeasure.Piece
                };
                db.Products.Add(product);
                db.StockMovements.AddRange(
                    CreateMovement(product, StockMovementType.Purchase, now.AddHours(-2), 5),
                    CreateMovement(product, StockMovementType.Purchase, now.AddHours(-1), 3),
                    CreateMovement(product, StockMovementType.Damage, now, -1));
                await db.SaveChangesAsync();
                productId = product.Id;
            }

            var response = await Client.GetAsync(
                $"/api/stock-movements?pageNumber=1&pageSize=1&productId={productId}" +
                $"&movementType={StockMovementType.Purchase}&fromDate={now.AddHours(-3):O}&toDate={now:O}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PagedResponse<Response>>();

            result.Should().NotBeNull();
            result!.TotalCount.Should().Be(2);
            result.Items.Should().ContainSingle();
            result.Items[0].QuantityChange.Should().Be(3);
            result.Items[0].OccurredAtUtc.Should().Be(now.AddHours(-1));
        }

        private static StockMovement CreateMovement(
            Product product,
            StockMovementType movementType,
            DateTime occurredAt,
            decimal quantityChange)
        {
            return new StockMovement
            {
                Product = product,
                MovementType = movementType,
                QuantityChange = quantityChange,
                BalanceBefore = 0,
                BalanceAfter = quantityChange,
                UnitCost = 10,
                SourceType = "Test",
                OccurredAtUtc = occurredAt,
                CreatedBy = "test"
            };
        }
    }
}
