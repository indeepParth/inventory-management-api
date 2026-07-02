using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.InventoryReports.GetPurchaseRegister;

public class HandlerTests
{
    [Fact]
    public async Task Handle_Should_Exclude_Draft_And_Cancelled_From_Default_Summary()
    {
        var repository = new Mock<IPurchaseRepository>();
        var documents = new List<Purchase>
        {
            Purchase(PurchaseStatus.Posted, 10m, 100m),
            Purchase(PurchaseStatus.Draft, 20m, 200m),
            Purchase(PurchaseStatus.Cancelled, 30m, 300m)
        };
        repository.Setup(x => x.GetRegisterAsync(
                1, 10, null, null, null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);
        repository.Setup(x => x.GetRegisterCountAsync(
                null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        repository.Setup(x => x.GetRegisterSummaryAsync(
                null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        var result = await new Handler(repository.Object)
            .Handle(new Query(), CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Summary.DocumentCount.Should().Be(1);
        result.Summary.TotalQuantity.Should().Be(10m);
        result.Summary.GrandTotal.Should().Be(100m);
    }

    private static Purchase Purchase(
        PurchaseStatus status,
        decimal quantity,
        decimal total) => new()
    {
        Status = status,
        GrandTotal = total,
        BalanceDue = total,
        Supplier = new Supplier(),
        Items = new List<PurchaseItem>
        {
            new() { Quantity = quantity, Product = new Product() }
        }
    };
}
