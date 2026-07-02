using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.InventoryReports.GetSalesRegister;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.InventoryReports.GetSalesRegister;

public class HandlerTests
{
    [Fact]
    public async Task Handle_Should_Filter_Source_And_Exclude_NonFinancial_Statuses_From_Summary()
    {
        var repository = new Mock<ISalesInvoiceRepository>();
        var documents = new List<SalesInvoice>
        {
            Invoice(SalesInvoiceStatus.Paid, 5m, 75m, true),
            Invoice(SalesInvoiceStatus.Draft, 9m, 90m, true)
        };
        repository.Setup(x => x.GetRegisterAsync(
                1, 10, null, null, true, null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);
        repository.Setup(x => x.GetRegisterCountAsync(
                null, null, true, null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        repository.Setup(x => x.GetRegisterSummaryAsync(
                null, null, true, null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        var result = await new Handler(repository.Object).Handle(
            new Query { SourceType = SalesSourceType.DeliveryChallan },
            CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(
            x => x.SourceType == SalesSourceType.DeliveryChallan);
        result.Summary.DocumentCount.Should().Be(1);
        result.Summary.TotalQuantity.Should().Be(5m);
        result.Summary.GrandTotal.Should().Be(75m);
    }

    private static SalesInvoice Invoice(
        SalesInvoiceStatus status,
        decimal quantity,
        decimal total,
        bool fromChallan) => new()
    {
        Status = status,
        GrandTotal = total,
        BalanceDue = total,
        Customer = new Customer(),
        Items = new List<SalesInvoiceItem>
        {
            new()
            {
                Quantity = quantity,
                DeliveryChallanItemId = fromChallan ? 1 : null,
                Product = new Product()
            }
        }
    };
}
