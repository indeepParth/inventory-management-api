using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;
using CreateCategoryCommand = InventoryManagement.Application.Features.Categories.CreateCategory.Command;
using CategoryResponse = InventoryManagement.Application.Features.Categories.Response;
using CreateProductCommand = InventoryManagement.Application.Features.Products.CreateProduct.Command;
using ProductResponse = InventoryManagement.Application.Features.Products.CreateProduct.Response;
using ConversionResponse = InventoryManagement.Application.Features.ProductUnitConversions.Response;
using CreateConversionCommand = InventoryManagement.Application.Features.ProductUnitConversions.CreateProductUnitConversion.Command;
using UpdateConversionCommand = InventoryManagement.Application.Features.ProductUnitConversions.UpdateProductUnitConversion.Command;

namespace InventoryManagement.Tests.IntegrationTests.ProductUnitConversions;

public class ProductUnitConversionEndpointsTests : TestBase
{
    public ProductUnitConversionEndpointsTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateProduct_Should_Create_BaseUnitConversion()
    {
        await AuthenticateAsync();
        var product = await CreateProductAsync(baseUnitId: 4);

        var conversions = await GetConversionsAsync(product.Id);

        conversions.Should().ContainSingle(x =>
            x.UnitId == 4 &&
            x.UnitName == "Piece" &&
            x.FactorToBaseUnit == 1m &&
            x.IsActive &&
            x.IsBaseUnit);
    }

    [Fact]
    public async Task CreateConversion_Should_Block_Duplicate_ProductUnit()
    {
        await AuthenticateAsync();
        var product = await CreateProductAsync(baseUnitId: 4);

        var command = new CreateConversionCommand
        {
            UnitId = 1,
            FactorToBaseUnit = 4m
        };

        var first = await Client.PostAsJsonAsync(
            $"/api/products/{product.Id}/unit-conversions",
            command);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await Client.PostAsJsonAsync(
            $"/api/products/{product.Id}/unit-conversions",
            command);
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAndDeactivate_Should_Work_For_NonBaseConversion()
    {
        await AuthenticateAsync();
        var product = await CreateProductAsync(baseUnitId: 4);
        var conversion = await CreateConversionAsync(product.Id, unitId: 1, factor: 4m);

        var update = await Client.PutAsJsonAsync(
            $"/api/products/{product.Id}/unit-conversions/{conversion.Id}",
            new UpdateConversionCommand(
                product.Id,
                conversion.Id,
                3.5m,
                true));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<ConversionResponse>();
        updated.Should().NotBeNull();
        updated!.FactorToBaseUnit.Should().Be(3.5m);
        updated.IsActive.Should().BeTrue();

        var deactivate = await Client.PatchAsync(
            $"/api/products/{product.Id}/unit-conversions/{conversion.Id}/deactivate",
            null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivated = await deactivate.Content.ReadFromJsonAsync<ConversionResponse>();
        deactivated.Should().NotBeNull();
        deactivated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task BaseConversion_Should_Not_Allow_NonOneFactor_Or_Deactivation()
    {
        await AuthenticateAsync();
        var product = await CreateProductAsync(baseUnitId: 4);
        var baseConversion = (await GetConversionsAsync(product.Id))
            .Single(x => x.IsBaseUnit);

        var update = await Client.PutAsJsonAsync(
            $"/api/products/{product.Id}/unit-conversions/{baseConversion.Id}",
            new UpdateConversionCommand(
                product.Id,
                baseConversion.Id,
                2m,
                true));
        update.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var deactivate = await Client.PatchAsync(
            $"/api/products/{product.Id}/unit-conversions/{baseConversion.Id}/deactivate",
            null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<ProductResponse> CreateProductAsync(int baseUnitId)
    {
        var category = await CreateCategoryAsync();
        var response = await Client.PostAsJsonAsync("/api/products", new CreateProductCommand
        {
            Name = $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            BaseUnitId = baseUnitId,
            DefaultSellingPrice = 10,
            CategoryId = category.Id
        });

        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product.Should().NotBeNull();
        return product!;
    }

    private async Task<CategoryResponse> CreateCategoryAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand
        {
            Name = $"Category {Guid.NewGuid():N}",
            Description = "Product conversion test"
        });

        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        category.Should().NotBeNull();
        return category!;
    }

    private async Task<ConversionResponse> CreateConversionAsync(
        int productId,
        int unitId,
        decimal factor)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/products/{productId}/unit-conversions",
            new CreateConversionCommand
            {
                UnitId = unitId,
                FactorToBaseUnit = factor
            });

        response.EnsureSuccessStatusCode();
        var conversion = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        conversion.Should().NotBeNull();
        return conversion!;
    }

    private async Task<List<ConversionResponse>> GetConversionsAsync(int productId)
    {
        var response = await Client.GetAsync($"/api/products/{productId}/unit-conversions");
        response.EnsureSuccessStatusCode();
        var conversions = await response.Content.ReadFromJsonAsync<List<ConversionResponse>>();
        conversions.Should().NotBeNull();
        return conversions!;
    }
}
