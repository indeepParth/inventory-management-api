using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;
using InventoryManagement.Domain.Enums;
using CreateCategoryCommand = InventoryManagement.Application.Features.Categories.CreateCategory.Command;
using CategoryResponse = InventoryManagement.Application.Features.Categories.Response;
using UpdateCategoryCommand = InventoryManagement.Application.Features.Categories.UpdateCategory.Command;
using CreateProductCommand = InventoryManagement.Application.Features.Products.CreateProduct.Command;

namespace InventoryManagement.Tests.IntegrationTests.Categories
{
    public class CategoriesTests : TestBase
    {
        public CategoriesTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateCategory_Should_Create_Category()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand
            {
                Name = $"Dairy {Guid.NewGuid():N}",
                Description = "Dairy products"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var result = await response.Content.ReadFromJsonAsync<CategoryResponse>();
            result.Should().NotBeNull();
            result!.Name.Should().StartWith("Dairy");
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateCategory_Should_Reject_Duplicate_Name()
        {
            await AuthenticateAsync();
            var name = $"Duplicate {Guid.NewGuid():N}";

            var request = new CreateCategoryCommand
            {
                Name = name,
                Description = "First"
            };

            await Client.PostAsJsonAsync("/api/categories", request);

            var response = await Client.PostAsJsonAsync("/api/categories", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Categories_Should_Support_Get_Update_And_Delete()
        {
            await AuthenticateAsync();
            var category = await CreateCategoryAsync();

            var getResponse = await Client.GetAsync($"/api/categories/{category.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updateResponse = await Client.PutAsJsonAsync($"/api/categories/{category.Id}", new UpdateCategoryCommand(
                category.Id,
                $"Updated {Guid.NewGuid():N}",
                "Updated description",
                true));

            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var deleteResponse = await Client.DeleteAsync($"/api/categories/{category.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DeleteCategory_Should_Be_Blocked_When_Category_Has_Products()
        {
            await AuthenticateAsync();
            var category = await CreateCategoryAsync();

            var productResponse = await Client.PostAsJsonAsync("/api/products", new CreateProductCommand
            {
                Name = "Milk",
                SKU = $"MILK-{Guid.NewGuid():N}",
                Quantity = 10,
                BaseUnit = UnitOfMeasure.Piece,
                DefaultSellingPrice = 50,
                CategoryId = category.Id
            });
            productResponse.EnsureSuccessStatusCode();

            var deleteResponse = await Client.DeleteAsync($"/api/categories/{category.Id}");

            deleteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private async Task<CategoryResponse> CreateCategoryAsync()
        {
            var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand
            {
                Name = $"Category {Guid.NewGuid():N}",
                Description = "Test category"
            });

            response.EnsureSuccessStatusCode();
            var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
            category.Should().NotBeNull();
            return category!;
        }
    }
}
