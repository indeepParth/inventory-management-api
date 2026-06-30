using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products.GetProducts;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Products.GetProducts
{
    public class HandlerTest
    {
        [Fact]
        public async Task Handle_Should_Return_Paged_Response()
        {
            var products = new List<Product>
            {
                new()
                {
                    Id = 1,
                    Name = "Test Product",
                    SKU = "TEST123",
                    Quantity = 10.125m,
                    BaseUnit = UnitOfMeasure.CubicFoot,
                    DefaultSellingPrice = 99.99m,
                    AverageCost = 70m,
                    CategoryId = 1,
                    Category = new Category
                    {
                        Id = 1,
                        Name = "Dairy"
                    }
                }
            };

            var repositoryMock = new Mock<IProductRepository>();
            repositoryMock
                .Setup(x => x.GetAllProductAsync(1, 10, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);
            repositoryMock
                .Setup(x => x.GetProductCountAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(21);

            var handler = new Handler(repositoryMock.Object);

            var result = await handler.Handle(new Query(), CancellationToken.None);

            result.Items.Should().HaveCount(1);
            result.Items.First().CategoryId.Should().Be(1);
            result.Items.First().CategoryName.Should().Be("Dairy");
            result.Items.First().Quantity.Should().Be(10.125m);
            result.Items.First().BaseUnit.Should().Be(UnitOfMeasure.CubicFoot);
            result.Items.First().DefaultSellingPrice.Should().Be(99.99m);
            result.Items.First().AverageCost.Should().Be(70m);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(21);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeTrue();
        }
    }
}
