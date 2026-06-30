using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.StockMovements.GetStockMovements;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.StockMovements.GetStockMovements
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Return_Filtered_Page_And_Map_Ledger_Fields()
        {
            var repository = new Mock<IStockMovementRepository>();
            var occurredAt = new DateTime(2026, 6, 30, 8, 0, 0, DateTimeKind.Utc);
            var query = new Query
            {
                PageNumber = 2,
                PageSize = 5,
                ProductId = 7,
                MovementType = StockMovementType.Purchase,
                FromDate = occurredAt.AddDays(-1),
                ToDate = occurredAt.AddDays(1)
            };
            repository.Setup(x => x.GetAsync(
                    query.PageNumber, query.PageSize, query.ProductId, query.MovementType,
                    query.FromDate, query.ToDate, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StockMovement>
                {
                    new()
                    {
                        Id = 11,
                        ProductId = 7,
                        Product = new Product { Name = "Keyboard" },
                        MovementType = StockMovementType.Purchase,
                        QuantityChange = 3,
                        BalanceBefore = 2,
                        BalanceAfter = 5,
                        UnitCost = 10,
                        SourceType = "Purchase",
                        SourceId = "42",
                        Reference = "PO-42",
                        Note = "Received",
                        OccurredAtUtc = occurredAt,
                        CreatedBy = "tester"
                    }
                });
            repository.Setup(x => x.CountAsync(
                    query.ProductId, query.MovementType, query.FromDate, query.ToDate,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(6);

            var result = await new Handler(repository.Object).Handle(query, CancellationToken.None);

            result.TotalCount.Should().Be(6);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(5);
            result.Items.Should().ContainSingle();
            result.Items[0].ProductName.Should().Be("Keyboard");
            result.Items[0].BalanceAfter.Should().Be(5);
            result.Items[0].Reference.Should().Be("PO-42");
        }
    }
}
