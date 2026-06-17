using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products.GetProductById;
using InventoryManagement.Domain.Entities;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Products.GetProductById
{
    public class HandlerTest
    {
        [Fact]
        public async Task GetProductByID_Should_Have_Product()
        {
            // ARRENGE
            var _repositoryMock = new Mock<IProductRepository>();
            _repositoryMock.Setup(x => x.GetProductByIdAsync(1))
                            .ReturnsAsync(new Product
                            {
                                Id = 1,
                                Name = "Test Product",
                                SKU = "TEST123",
                                Quantity = 10,
                                Price = 99.99m
                            });

            Query request = new Query { Id = 1 };

            Handler handler = new Handler(_repositoryMock.Object);

            CancellationToken cancellationToken = new CancellationToken();

            // ACT
            var product = await handler.Handle(request, cancellationToken);

            product.Should().NotBeNull();
            product.Id.Should().Be(1);
            product.Name.Should().Be("Test Product");
            product.SKU.Should().Be("TEST123");
            product.Quantity.Should().Be(10);
            product.Price.Should().Be(99.99m);

            // ASSERT
            _repositoryMock.Verify(
                x => x.GetProductByIdAsync(1),
                Times.Once
            );
        }

        [Fact]
        public async Task GetProductById_Should_Throw_NotFoundException()
        {
            var repositoryMock = new Mock<IProductRepository>();
            repositoryMock.Setup(x => x.GetProductByIdAsync(It.IsAny<int>()))
                            .ReturnsAsync((Product?)null);

            Query request = new Query { Id = 1 };

            Handler handler = new Handler(repositoryMock.Object);

            CancellationToken cancellationToken = new CancellationToken();

            await Assert.ThrowsAsync<NotFoundException>(()
                        => handler.Handle(request, cancellationToken));

            repositoryMock.Verify(
                x => x.GetProductByIdAsync(1),
                Times.Once);
        }
    }
}