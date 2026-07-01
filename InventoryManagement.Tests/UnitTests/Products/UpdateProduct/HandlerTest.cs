using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products.UpdateProduct;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Products.UpdateProduct
{
    public class HandlerTest
    {
        [Fact]
        public void Command_Should_Not_Expose_Stock_Fields()
        {
            typeof(Command).GetProperty("Quantity").Should().BeNull();
            typeof(Command).GetProperty("AverageCost").Should().BeNull();
            typeof(Command).GetProperty("SupplierId").Should().BeNull();
            typeof(Response).GetProperty("SupplierId").Should().BeNull();
            typeof(Response).GetProperty("SupplierName").Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Update_Metadata_Without_Changing_Stock()
        {
            // Arrange
            // CancellationToken cancellationToken = new CancellationToken();
            var existingProduct = new Product
            {
                Id = 1,
                Name = "Old Product",
                SKU = "OLD123",
                Quantity = 5.500m,
                BaseUnit = UnitOfMeasure.Bag,
                DefaultSellingPrice = 50,
                AverageCost = 35,
                CategoryId = 1,
                Category = new Category
                {
                    Id = 1,
                    Name = "Old Category"
                }
            };
            var category = new Category
            {
                Id = 2,
                Name = "New Category"
            };

            var updateDto = new Command
            (
                1,
                "Updated Product",
                "NEW123",
                UnitOfMeasure.Kilogram,
                150,
                2
            );


            var _repositoryMock = new Mock<IProductRepository>();
            var categoryRepositoryMock = new Mock<ICategoryRepository>();
            categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);
            _repositoryMock.Setup(x => x.GetProductByIdAsync(1, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(existingProduct);
            _repositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.CompletedTask);

            var handler = new Handler(
                _repositoryMock.Object,
                categoryRepositoryMock.Object);

            // ACT
            await handler.Handle(updateDto, It.IsAny<CancellationToken>());

            existingProduct.Should().NotBeNull();
            existingProduct.Name.Should().Be("Updated Product");
            existingProduct.SKU.Should().Be("NEW123");
            existingProduct.Quantity.Should().Be(5.500m);
            existingProduct.BaseUnit.Should().Be(UnitOfMeasure.Kilogram);
            existingProduct.DefaultSellingPrice.Should().Be(150);
            existingProduct.AverageCost.Should().Be(35);
            existingProduct.CategoryId.Should().Be(2);

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
