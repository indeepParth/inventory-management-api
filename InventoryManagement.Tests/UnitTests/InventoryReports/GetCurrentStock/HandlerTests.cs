using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.InventoryReports.GetCurrentStock;

public class HandlerTests
{
    [Fact]
    public async Task Handle_Should_Calculate_StockValue_From_Quantity_And_AverageCost()
    {
        var repository = new Mock<IProductRepository>();
        var query = new Query { PageNumber = 1, PageSize = 20 };
        repository.Setup(x => x.GetCurrentStockReportAsync(
                1, 20, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>
            {
                new()
                {
                    Id = 4,
                    Name = "Cement",
                    Category = new Category { Name = "Building Materials" },
                    BaseUnit = UnitOfMeasure.Bag,
                    Quantity = 12.5m,
                    AverageCost = 420.40m,
                    DefaultSellingPrice = 475m
                }
            });
        repository.Setup(x => x.GetCurrentStockReportCountAsync(
                null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await new Handler(repository.Object)
            .Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].StockValue.Should().Be(5255.000m);
        result.Items[0].Quantity.Should().Be(12.5m);
        result.Items[0].AverageCost.Should().Be(420.40m);
    }
}
