using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.InventoryReports.GetProductStockLedger;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.InventoryReports.GetProductStockLedger;

public class HandlerTests
{
    [Fact]
    public async Task Handle_Should_Use_Persisted_BalanceAfter_As_RunningBalance()
    {
        var repository = new Mock<IStockMovementRepository>();
        var query = new Query { ProductId = 8, PageNumber = 1, PageSize = 10 };
        repository.Setup(x => x.GetAsync(
                1, 10, 8, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>
            {
                new()
                {
                    Id = 21,
                    ProductId = 8,
                    BalanceAfter = 17.25m,
                    QuantityChange = -2.75m,
                    MovementType = StockMovementType.Sale,
                    SourceType = "SalesInvoice",
                    SourceId = "5",
                    Reference = "INV-005"
                }
            });
        repository.Setup(x => x.CountAsync(
                8, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await new Handler(repository.Object)
            .Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].RunningBalance.Should().Be(17.25m);
        result.Items[0].QuantityChange.Should().Be(-2.75m);
        result.Items[0].SourceReference.Should().Be("INV-005");
    }
}
