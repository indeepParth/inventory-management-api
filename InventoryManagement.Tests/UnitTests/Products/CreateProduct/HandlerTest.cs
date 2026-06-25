using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products.CreateProduct;
using InventoryManagement.Domain.Entities;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Products.CreateProduct
{
    public class HandlerTest
    {
        [Fact]
        public async Task Handle_Should_Create_Product()
        {
            // Arrange
            Product? addedProduct = null;
            // CancellationToken cancellationToken = new CancellationToken();
            
            var _repositoryMock = new Mock<IProductRepository>();
            _repositoryMock.Setup(x => x.AddProductAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                            .Callback<Product, CancellationToken>((p,ct) => addedProduct = p)
                            .Returns(Task.CompletedTask);
            _repositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = new Handler(_repositoryMock.Object);

            var newProduct = new Command
            {
                Name = "Test Product",
                SKU = "TEST123",
                Quantity = 10,
                Price = 99.99m
            };

            // ACT
            await handler.Handle(newProduct, It.IsAny<CancellationToken>());

            addedProduct.Should().NotBeNull();
            addedProduct.Name.Should().Be(newProduct.Name);
            addedProduct.SKU.Should().Be(newProduct.SKU);
            addedProduct.Quantity.Should().Be(newProduct.Quantity);
            addedProduct.Price.Should().Be(newProduct.Price);

            // ASSERT            
            _repositoryMock.Verify(
                x => x.AddProductAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _repositoryMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}