using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using InventoryManagement.Application.Common.Exceptions;
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
            var category = new Category
            {
                Id = 1,
                Name = "Dairy"
            };
            var unit = new Unit
            {
                Id = 2,
                Name = "Kilogram",
                BaseUnitId = 2,
                FactorToBaseUnit = 1m,
                IsActive = true
            };
            // CancellationToken cancellationToken = new CancellationToken();
            
            var _repositoryMock = new Mock<IProductRepository>();
            var categoryRepositoryMock = new Mock<ICategoryRepository>();
            var unitRepositoryMock = new Mock<IUnitRepository>();
            categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
            unitRepositoryMock
                .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unit);
            _repositoryMock.Setup(x => x.AddProductAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                            .Callback<Product, CancellationToken>((p,ct) => addedProduct = p)
                            .Returns(Task.CompletedTask);
            _repositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);
            var handler = new Handler(
                _repositoryMock.Object,
                categoryRepositoryMock.Object,
                unitRepositoryMock.Object);

            var newProduct = new Command
            {
                Name = "Test Product",
                SKU = "TEST123",
                BaseUnitId = 2,
                DefaultSellingPrice = 99.99m,
                CategoryId = 1
            };

            // ACT
            await handler.Handle(newProduct, It.IsAny<CancellationToken>());

            addedProduct.Should().NotBeNull();
            addedProduct.Name.Should().Be(newProduct.Name);
            addedProduct.SKU.Should().Be(newProduct.SKU);
            addedProduct.Quantity.Should().Be(0m);
            addedProduct.BaseUnitId.Should().Be(newProduct.BaseUnitId);
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

        [Fact]
        public async Task Handle_Should_Create_SubProduct_With_BaseProductFactor()
        {
            Product? addedProduct = null;
            var category = new Category { Id = 1, Name = "Aggregate" };
            var unit = new Unit
            {
                Id = 1,
                Name = "Ton",
                BaseUnitId = 1,
                FactorToBaseUnit = 1m,
                IsActive = true
            };
            var baseProduct = new Product
            {
                Id = 10,
                Name = "Kapchi",
                SKU = "KAPCHI",
                Quantity = 100m,
                BaseUnitId = 1,
                BaseUnit = unit,
                CategoryId = 1,
                Category = category
            };

            var repositoryMock = new Mock<IProductRepository>();
            var categoryRepositoryMock = new Mock<ICategoryRepository>();
            var unitRepositoryMock = new Mock<IUnitRepository>();
            categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
            unitRepositoryMock
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unit);
            repositoryMock
                .Setup(x => x.GetProductByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(baseProduct);
            repositoryMock
                .Setup(x => x.AddProductAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .Callback<Product, CancellationToken>((p, ct) => addedProduct = p)
                .Returns(Task.CompletedTask);
            repositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new Handler(
                repositoryMock.Object,
                categoryRepositoryMock.Object,
                unitRepositoryMock.Object);

            var response = await handler.Handle(new Command
            {
                Name = "Kapchi Tractor",
                SKU = "KAPCHI-TRACTOR",
                BaseUnitId = 1,
                BaseProductId = 10,
                FactorToBaseProduct = 10m,
                DefaultSellingPrice = 2500m,
                CategoryId = 1
            }, CancellationToken.None);

            addedProduct.Should().NotBeNull();
            addedProduct!.BaseProductId.Should().Be(10);
            addedProduct.FactorToBaseProduct.Should().Be(10m);
            response.IsSubProduct.Should().BeTrue();
            response.AvailableQuantity.Should().Be(10m);
        }

        [Fact]
        public async Task Handle_Should_Reject_Nested_SubProduct()
        {
            var category = new Category { Id = 1, Name = "Aggregate" };
            var unit = new Unit
            {
                Id = 1,
                Name = "Ton",
                BaseUnitId = 1,
                FactorToBaseUnit = 1m,
                IsActive = true
            };
            var subProduct = new Product
            {
                Id = 10,
                Name = "Kapchi Tractor",
                BaseProductId = 5,
                BaseUnitId = 1,
                BaseUnit = unit
            };

            var repositoryMock = new Mock<IProductRepository>();
            var categoryRepositoryMock = new Mock<ICategoryRepository>();
            var unitRepositoryMock = new Mock<IUnitRepository>();
            categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
            unitRepositoryMock
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unit);
            repositoryMock
                .Setup(x => x.GetProductByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subProduct);

            var handler = new Handler(
                repositoryMock.Object,
                categoryRepositoryMock.Object,
                unitRepositoryMock.Object);

            var command = new Command
            {
                Name = "Nested Product",
                SKU = "NESTED",
                BaseUnitId = 1,
                BaseProductId = 10,
                FactorToBaseProduct = 2m,
                DefaultSellingPrice = 100m,
                CategoryId = 1
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                handler.Handle(command, CancellationToken.None));
        }
    }
}
