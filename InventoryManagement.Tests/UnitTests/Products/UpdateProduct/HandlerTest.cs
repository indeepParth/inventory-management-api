using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products.UpdateProduct;
using InventoryManagement.Domain.Entities;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Products.UpdateProduct
{
    public class HandlerTest
    {
        [Fact]
        public async Task Handle_Should_Update_Product()
        {
            // Arrange
            // CancellationToken cancellationToken = new CancellationToken();
            var existingProduct = new Product
            {
                Id = 1,
                Name = "Old Product",
                SKU = "OLD123",
                Quantity = 5,
                Price = 50
            };

            var updateDto = new Command
            (
                1,
                "Updated Product",
                "NEW123",
                20,
                150
            );


            var _repositoryMock = new Mock<IProductRepository>();
            _repositoryMock.Setup(x => x.GetProductByIdAsync(1, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(existingProduct);
            _repositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = new Handler(_repositoryMock.Object);

            // ACT
            await handler.Handle(updateDto, It.IsAny<CancellationToken>());

            existingProduct.Should().NotBeNull();
            existingProduct.Name.Should().Be("Updated Product");
            existingProduct.SKU.Should().Be("NEW123");
            existingProduct.Quantity.Should().Be(20);
            existingProduct.Price.Should().Be(150);

            // ASSERT            
            _repositoryMock.Verify(
                x => x.GetProductByIdAsync(1, It.IsAny<CancellationToken>()),
                Times.Once);

            _repositoryMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}