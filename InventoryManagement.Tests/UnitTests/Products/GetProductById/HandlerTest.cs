using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products.GetProductById;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Products.GetProductById
{
    public class HandlerTest
    {
        [Fact]
        public async Task GetProductByID_Should_Have_Product()
        {
            // ARRENGE
            CancellationToken cancellationToken = new CancellationToken();
            var _repositoryMock = new Mock<IProductRepository>();
            _repositoryMock.Setup(x => x.GetProductByIdAsync(1, cancellationToken))
                            .ReturnsAsync(new Product
                            {
                                Id = 1,
                                Name = "Test Product",
                                SKU = "TEST123",
                                Quantity = 10.125m,
                                BaseUnit = UnitOfMeasure.CubicMeter,
                                DefaultSellingPrice = 99.99m,
                                AverageCost = 75.50m,
                                CategoryId = 1,
                                Category = new Category
                                {
                                    Id = 1,
                                    Name = "Dairy"
                                }
                            });

            Query request = new Query { Id = 1 };

            Handler handler = new Handler(_repositoryMock.Object);

            // ACT
            var product = await handler.Handle(request, cancellationToken);

            product.Should().NotBeNull();
            product.Id.Should().Be(1);
            product.Name.Should().Be("Test Product");
            product.SKU.Should().Be("TEST123");
            product.Quantity.Should().Be(10.125m);
            product.BaseUnit.Should().Be(UnitOfMeasure.CubicMeter);
            product.DefaultSellingPrice.Should().Be(99.99m);
            product.AverageCost.Should().Be(75.50m);
            product.CategoryId.Should().Be(1);
            product.CategoryName.Should().Be("Dairy");

            // ASSERT
            _repositoryMock.Verify(
                x => x.GetProductByIdAsync(1, cancellationToken),
                Times.Once
            );
        }

        [Fact]
        public async Task GetProductById_Should_Throw_NotFoundException()
        {
            CancellationToken cancellationToken = new CancellationToken();
            var repositoryMock = new Mock<IProductRepository>();
            repositoryMock.Setup(x => x.GetProductByIdAsync(It.IsAny<int>(), cancellationToken))
                            .ReturnsAsync((Product?)null);

            Query request = new Query { Id = 1 };

            Handler handler = new Handler(repositoryMock.Object);

            await Assert.ThrowsAsync<NotFoundException>(()
                        => handler.Handle(request, cancellationToken));

            repositoryMock.Verify(
                x => x.GetProductByIdAsync(1, cancellationToken),
                Times.Once);
        }
    }
}
