using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products.CreateProduct;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
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
            var category = new Category
            {
                Id = 1,
                Name = "Dairy"
            };
            // CancellationToken cancellationToken = new CancellationToken();
            
            var _repositoryMock = new Mock<IProductRepository>();
            var categoryRepositoryMock = new Mock<ICategoryRepository>();
            categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
            _repositoryMock.Setup(x => x.AddProductAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                            .Callback<Product, CancellationToken>((p,ct) => addedProduct = p)
                            .Returns(Task.CompletedTask);
            _repositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = new Handler(
                _repositoryMock.Object,
                categoryRepositoryMock.Object);

            var newProduct = new Command
            {
                Name = "Test Product",
                SKU = "TEST123",
                BaseUnit = UnitOfMeasure.Kilogram,
                DefaultSellingPrice = 99.99m,
                CategoryId = 1
            };

            // ACT
            await handler.Handle(newProduct, It.IsAny<CancellationToken>());

            addedProduct.Should().NotBeNull();
            addedProduct.Name.Should().Be(newProduct.Name);
            addedProduct.SKU.Should().Be(newProduct.SKU);
            addedProduct.Quantity.Should().Be(0m);
            addedProduct.BaseUnit.Should().Be(newProduct.BaseUnit);
            addedProduct.DefaultSellingPrice.Should().Be(newProduct.DefaultSellingPrice);
            addedProduct.AverageCost.Should().Be(0m);
            addedProduct.CategoryId.Should().Be(newProduct.CategoryId);
            typeof(Command).GetProperty("SupplierId").Should().BeNull();
            typeof(Response).GetProperty("SupplierId").Should().BeNull();
            typeof(Response).GetProperty("SupplierName").Should().BeNull();

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
