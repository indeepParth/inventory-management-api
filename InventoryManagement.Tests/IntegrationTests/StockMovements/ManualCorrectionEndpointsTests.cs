using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.StockMovements;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AdjustmentCommand =
    InventoryManagement.Application.Features.StockMovements.RecordAdjustment.Command;
using DamageCommand =
    InventoryManagement.Application.Features.StockMovements.RecordDamage.Command;
using ReversalCommand =
    InventoryManagement.Application.Features.StockMovements.ReverseManualCorrection.Command;

namespace InventoryManagement.Tests.IntegrationTests.StockMovements
{
    public class ManualCorrectionEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public ManualCorrectionEndpointsTests(
            CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Damage_Should_Require_Authentication()
        {
            var response = await Client.PostAsJsonAsync(
                "/api/stock-movements/damage",
                new DamageCommand
                {
                    ProductId = 1,
                    Quantity = 1,
                    Reason = "Broken"
                });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Damage_Should_Record_Negative_Movement_At_Average_Cost()
        {
            await AuthenticateAsync();
            var productId = await SeedProductAsync(10, 25);

            var response = await Client.PostAsJsonAsync(
                "/api/stock-movements/damage",
                new DamageCommand
                {
                    ProductId = productId,
                    Quantity = 2.5m,
                    Reason = " Damaged in storage ",
                    Reference = " REF-1 ",
                    Note = " Box crushed "
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var movement = await response.Content
                .ReadFromJsonAsync<ManualCorrectionResponse>();
            movement.Should().NotBeNull();
            movement!.MovementType.Should().Be(StockMovementType.Damage);
            movement.QuantityChange.Should().Be(-2.5m);
            movement.BalanceBefore.Should().Be(10);
            movement.BalanceAfter.Should().Be(7.5m);
            movement.UnitCost.Should().Be(25);
            movement.Reason.Should().Be("Damaged in storage");
            movement.Reference.Should().Be("REF-1");
            movement.Note.Should().Be("Box crushed");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            (await db.Products.SingleAsync(x => x.Id == productId))
                .Quantity.Should().Be(7.5m);
        }

        [Theory]
        [InlineData(3, 13)]
        [InlineData(-3, 7)]
        public async Task Adjustment_Should_Accept_Signed_Quantity(
            decimal quantityChange,
            decimal expectedStock)
        {
            await AuthenticateAsync();
            var productId = await SeedProductAsync(10, 18);

            var response = await Client.PostAsJsonAsync(
                "/api/stock-movements/adjustment",
                new AdjustmentCommand
                {
                    ProductId = productId,
                    QuantityChange = quantityChange,
                    Reason = "Cycle count"
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var movement = await response.Content
                .ReadFromJsonAsync<ManualCorrectionResponse>();
            movement!.MovementType.Should().Be(StockMovementType.Adjustment);
            movement.QuantityChange.Should().Be(quantityChange);
            movement.BalanceAfter.Should().Be(expectedStock);
            movement.UnitCost.Should().Be(18);
        }

        [Fact]
        public async Task Correction_Should_Reject_Missing_Reason_And_Negative_Stock()
        {
            await AuthenticateAsync();
            var productId = await SeedProductAsync(2, 10);
            var missingReason = await Client.PostAsJsonAsync(
                "/api/stock-movements/damage",
                new DamageCommand
                {
                    ProductId = productId,
                    Quantity = 1,
                    Reason = " "
                });
            missingReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await missingReason.Content.ReadAsStringAsync())
                .Should().Contain("errors");

            var belowZero = await Client.PostAsJsonAsync(
                "/api/stock-movements/adjustment",
                new AdjustmentCommand
                {
                    ProductId = productId,
                    QuantityChange = -3,
                    Reason = "Count correction"
                });
            belowZero.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await belowZero.Content.ReadAsStringAsync())
                .Should().Contain("traceId");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            (await db.Products.SingleAsync(x => x.Id == productId))
                .Quantity.Should().Be(2);
            (await db.StockMovements.CountAsync(x =>
                x.ProductId == productId &&
                x.SourceType == "ManualCorrection")).Should().Be(0);
        }

        [Fact]
        public async Task Reverse_Should_Append_Compensating_Movement_Only_Once()
        {
            await AuthenticateAsync();
            var productId = await SeedProductAsync(10, 25);
            var damageResponse = await Client.PostAsJsonAsync(
                "/api/stock-movements/damage",
                new DamageCommand
                {
                    ProductId = productId,
                    Quantity = 2,
                    Reason = "Damaged"
                });
            damageResponse.EnsureSuccessStatusCode();
            var damage = await damageResponse.Content
                .ReadFromJsonAsync<ManualCorrectionResponse>();

            var command = new ReversalCommand
            {
                Reason = "Entered against wrong product",
                Reference = "REV-1",
                Note = "Approved"
            };
            var first = await Client.PostAsJsonAsync(
                $"/api/stock-movements/{damage!.Id}/reverse",
                command);
            var second = await Client.PostAsJsonAsync(
                $"/api/stock-movements/{damage.Id}/reverse",
                command);

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var reversal = await first.Content
                .ReadFromJsonAsync<ManualCorrectionResponse>();
            reversal!.MovementType.Should().Be(StockMovementType.Reversal);
            reversal.QuantityChange.Should().Be(2);
            reversal.SourceId.Should().Be(damage.Id.ToString());
            reversal.UnitCost.Should().Be(25);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            (await db.Products.SingleAsync(x => x.Id == productId))
                .Quantity.Should().Be(10);
            (await db.StockMovements.CountAsync(x =>
                x.SourceType == "ManualCorrectionReversal" &&
                x.SourceId == damage.Id.ToString())).Should().Be(1);
        }

        private async Task<int> SeedProductAsync(
            decimal quantity,
            decimal averageCost)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var product = new Product
            {
                Name = $"Correction product {suffix}",
                SKU = $"COR-{suffix}",
                Quantity = quantity,
                AverageCost = averageCost,
                Category = new Category
                {
                    Name = $"Correction category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product.Id;
        }
    }
}
