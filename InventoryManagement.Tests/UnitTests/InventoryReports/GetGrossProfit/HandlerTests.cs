using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.InventoryReports.GetGrossProfit;
using Moq;

namespace InventoryManagement.Tests.UnitTests.InventoryReports.GetGrossProfit;

public class HandlerTests
{
    [Fact]
    public async Task Handle_Should_Use_Historical_Costs_And_Subtract_Returns()
    {
        var repository = new Mock<IGrossProfitReportRepository>();
        repository.Setup(x => x.GetTransactionsAsync(
                null, null, null, null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                Transaction(
                    invoiceId: 1,
                    productId: 1,
                    quantity: 10m,
                    sellingPrice: 20m,
                    historicalPurchaseCost: 10m),
                Transaction(
                    invoiceId: 2,
                    productId: 1,
                    quantity: 4m,
                    sellingPrice: 35m,
                    historicalPurchaseCost: 20m),
                Transaction(
                    invoiceId: 1,
                    productId: 1,
                    quantity: 2m,
                    sellingPrice: 20m,
                    historicalPurchaseCost: 10m,
                    isReturn: true)
            });

        var result = await new Handler(repository.Object).Handle(
            new Query(),
            CancellationToken.None);

        result.ReportName.Should().Be("Gross Profit");
        result.Summary.SoldQuantity.Should().Be(14m);
        result.Summary.ReturnedQuantity.Should().Be(2m);
        result.Summary.NetQuantity.Should().Be(12m);
        result.Summary.Revenue.Should().Be(340m);
        result.Summary.Returns.Should().Be(40m);
        result.Summary.NetRevenue.Should().Be(300m);
        result.Summary.CostOfGoodsSold.Should().Be(180m);
        result.Summary.ReturnedCost.Should().Be(20m);
        result.Summary.NetCostOfGoodsSold.Should().Be(160m);
        result.Summary.GrossProfit.Should().Be(140m);
        result.Summary.GrossMarginPercentage.Should().Be(46.67m);
        result.ByInvoice.Should().HaveCount(2);
        result.ByProduct.Should().ContainSingle()
            .Which.GrossProfit.Should().Be(140m);
        result.ByCategory.Should().ContainSingle();
        result.ByCustomer.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Should_Forward_All_Report_Filters()
    {
        var repository = new Mock<IGrossProfitReportRepository>();
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        repository.Setup(x => x.GetTransactionsAsync(
                from, to, 1, 2, 3, 4,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GrossProfitTransaction>());

        await new Handler(repository.Object).Handle(new Query
        {
            FromDate = from,
            ToDate = to,
            InvoiceId = 1,
            ProductId = 2,
            CategoryId = 3,
            CustomerId = 4
        }, CancellationToken.None);

        repository.Verify(x => x.GetTransactionsAsync(
            from, to, 1, 2, 3, 4,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static GrossProfitTransaction Transaction(
        int invoiceId,
        int productId,
        decimal quantity,
        decimal sellingPrice,
        decimal historicalPurchaseCost,
        bool isReturn = false) => new()
    {
        Date = new DateTime(2026, 1, 10),
        InvoiceId = invoiceId,
        InvoiceNumber = $"INV-{invoiceId}",
        ProductId = productId,
        ProductName = "Product",
        CategoryId = 1,
        CategoryName = "Category",
        CustomerId = 1,
        CustomerName = "Customer",
        Quantity = quantity,
        SellingUnitPrice = sellingPrice,
        CostAtSale = historicalPurchaseCost,
        IsReturn = isReturn
    };
}
