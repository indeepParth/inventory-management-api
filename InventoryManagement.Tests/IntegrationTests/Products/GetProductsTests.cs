using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;
using InventoryManagement.Domain.Enums;
using ProductListResponse = InventoryManagement.Application.Common.Models.PagedResponse<InventoryManagement.Application.Features.Products.GetProducts.Response>;
using CreateCategoryCommand = InventoryManagement.Application.Features.Categories.CreateCategory.Command;
using CategoryResponse = InventoryManagement.Application.Features.Categories.Response;
using CreateProductCommand = InventoryManagement.Application.Features.Products.CreateProduct.Command;

namespace InventoryManagement.Tests.IntegrationTests.Products
{
    public class GetProductsTests : TestBase
    {
        public GetProductsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetProducts_Should_Return_Pagination_Metadata()
        {
            await AuthenticateAsync();
            var category = await CreateCategoryAsync();

            var createResponse = await Client.PostAsJsonAsync("/api/products", new CreateProductCommand
            {
                Name = "Keyboard",
                SKU = $"SKU-{Guid.NewGuid():N}",
                BaseUnit = UnitOfMeasure.Piece,
                DefaultSellingPrice = 49.99m,
                CategoryId = category.Id
            });
            createResponse.EnsureSuccessStatusCode();

            var response = await Client.GetAsync("/api/products?pageNumber=1&pageSize=10");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();

            result.Should().NotBeNull();
            result!.Items.Should().NotBeEmpty();
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
            result.TotalPages.Should().BeGreaterThanOrEqualTo(1);
            result.HasPreviousPage.Should().BeFalse();
            result.Items.First().CategoryId.Should().Be(category.Id);
            result.Items.First().CategoryName.Should().Be(category.Name);
            result.Items.First().Quantity.Should().Be(0m);
            result.Items.First().BaseUnit.Should().Be(UnitOfMeasure.Piece.ToString());
            result.Items.First().DefaultSellingPrice.Should().Be(49.99m);
            result.Items.First().AverageCost.Should().Be(0m);
        }

        private async Task<CategoryResponse> CreateCategoryAsync()
        {
            var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand
            {
                Name = $"Hardware {Guid.NewGuid():N}",
                Description = "Hardware products"
            });

            response.EnsureSuccessStatusCode();
            var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
            category.Should().NotBeNull();
            return category!;
        }
    }
}
