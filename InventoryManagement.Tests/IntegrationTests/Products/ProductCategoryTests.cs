using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;
using InventoryManagement.Domain.Enums;
using CreateCategoryCommand = InventoryManagement.Application.Features.Categories.CreateCategory.Command;
using CategoryResponse = InventoryManagement.Application.Features.Categories.Response;
using CreateProductCommand = InventoryManagement.Application.Features.Products.CreateProduct.Command;
using CreateProductResponse = InventoryManagement.Application.Features.Products.CreateProduct.Response;
using UpdateProductCommand = InventoryManagement.Application.Features.Products.UpdateProduct.Command;

namespace InventoryManagement.Tests.IntegrationTests.Products
{
    public class ProductCategoryTests : TestBase
    {
        public ProductCategoryTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateProduct_Should_Require_CategoryId()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync("/api/products", new CreateProductCommand
            {
                Name = "Uncategorized product",
                SKU = $"SKU-{Guid.NewGuid():N}",
                BaseUnit = UnitOfMeasure.Piece,
                DefaultSellingPrice = 10
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateProduct_Should_Reject_Invalid_CategoryId()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync("/api/products", new CreateProductCommand
            {
                Name = "Invalid category product",
                SKU = $"SKU-{Guid.NewGuid():N}",
                BaseUnit = UnitOfMeasure.Piece,
                DefaultSellingPrice = 10,
                CategoryId = int.MaxValue
            });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateProduct_Should_Reject_Invalid_CategoryId()
        {
            await AuthenticateAsync();
            var category = await CreateCategoryAsync();

            var createResponse = await Client.PostAsJsonAsync("/api/products", new CreateProductCommand
            {
                Name = "Product to update",
                SKU = $"SKU-{Guid.NewGuid():N}",
                BaseUnit = UnitOfMeasure.Piece,
                DefaultSellingPrice = 10,
                CategoryId = category.Id
            });
            createResponse.EnsureSuccessStatusCode();

            var product = await createResponse.Content.ReadFromJsonAsync<CreateProductResponse>();
            product.Should().NotBeNull();

            var updateResponse = await Client.PutAsJsonAsync(
                $"/api/products/{product!.Id}",
                new UpdateProductCommand(
                    product.Id,
                    product.Name,
                    product.SKU,
                    product.BaseUnit,
                    product.DefaultSellingPrice,
                    int.MaxValue,
                    product.SupplierId));

            updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private async Task<CategoryResponse> CreateCategoryAsync()
        {
            var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand
            {
                Name = $"Category {Guid.NewGuid():N}",
                Description = "Product category test"
            });

            response.EnsureSuccessStatusCode();
            var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
            category.Should().NotBeNull();
            return category!;
        }
    }
}
