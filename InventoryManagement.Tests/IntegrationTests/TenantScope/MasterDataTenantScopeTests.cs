using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Application.Features.Customers;
using InventoryManagement.Tests.IntegrationTests.Common;
using CategoryResponse = InventoryManagement.Application.Features.Categories.Response;
using CreateCategoryCommand = InventoryManagement.Application.Features.Categories.CreateCategory.Command;
using CreateCompanyRequest = InventoryManagement.API.Controllers.CreateCompanyRequest;
using CreateCustomerCommand = InventoryManagement.Application.Features.Customers.CreateCustomer.Command;
using CreateProductCommand = InventoryManagement.Application.Features.Products.CreateProduct.Command;
using ProductListResponse = InventoryManagement.Application.Common.Models.PagedResponse<InventoryManagement.Application.Features.Products.GetProducts.Response>;

namespace InventoryManagement.Tests.IntegrationTests.TenantScope
{
    public class MasterDataTenantScopeTests : TestBase
    {
        public MasterDataTenantScopeTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Master_Data_Should_Be_Isolated_By_Active_Company()
        {
            await AuthenticateAndCreateCompanyAsync();
            await SetActiveCompanyBillingPlanLimitsAsync(maxCompanies: 2);
            var firstCompanyId = ActiveCompanyId;
            var first = await SeedMasterDataAsync("Shared master", "27ABCDE1234F1Z5");

            var createCompanyResponse = await Client.PostAsJsonAsync(
                "/api/companies",
                new CreateCompanyRequest
                {
                    Name = $"Second company {Guid.NewGuid():N}"
                });
            createCompanyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var secondCompany = await createCompanyResponse.Content
                .ReadFromJsonAsync<UserCompanyDto>();
            secondCompany.Should().NotBeNull();

            SetActiveCompanyId(secondCompany!.Id);
            var second = await SeedMasterDataAsync("Shared master", "27ABCDE1234F1Z5");

            var secondCategories = await Client
                .GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
            secondCategories.Should().NotBeNull();
            secondCategories!.Select(x => x.Id).Should().Contain(second.CategoryId);
            secondCategories.Select(x => x.Id).Should().NotContain(first.CategoryId);

            var secondCustomers = await Client
                .GetFromJsonAsync<PagedResponse<CustomerResponse>>("/api/customers");
            secondCustomers.Should().NotBeNull();
            secondCustomers!.Items.Select(x => x.Id).Should().Contain(second.CustomerId);
            secondCustomers.Items.Select(x => x.Id).Should().NotContain(first.CustomerId);

            var secondProducts = await Client
                .GetFromJsonAsync<ProductListResponse>("/api/products");
            secondProducts.Should().NotBeNull();
            secondProducts!.Items.Select(x => x.Id).Should().Contain(second.ProductId);
            secondProducts.Items.Select(x => x.Id).Should().NotContain(first.ProductId);

            SetActiveCompanyId(firstCompanyId);

            var firstProducts = await Client
                .GetFromJsonAsync<ProductListResponse>("/api/products");
            firstProducts.Should().NotBeNull();
            firstProducts!.Items.Select(x => x.Id).Should().Contain(first.ProductId);
            firstProducts.Items.Select(x => x.Id).Should().NotContain(second.ProductId);
        }

        private async Task<SeededMasterData> SeedMasterDataAsync(
            string sharedName,
            string sharedGstNumber)
        {
            var categoryResponse = await Client.PostAsJsonAsync(
                "/api/categories",
                new CreateCategoryCommand
                {
                    Name = sharedName,
                    Description = "Tenant scoped category"
                });
            categoryResponse.EnsureSuccessStatusCode();
            var category = await categoryResponse.Content
                .ReadFromJsonAsync<CategoryResponse>();
            category.Should().NotBeNull();

            var customerResponse = await Client.PostAsJsonAsync(
                "/api/customers",
                new CreateCustomerCommand
                {
                    Name = sharedName,
                    GstNumber = sharedGstNumber
                });
            customerResponse.EnsureSuccessStatusCode();
            var customer = await customerResponse.Content
                .ReadFromJsonAsync<CustomerResponse>();
            customer.Should().NotBeNull();

            var productResponse = await Client.PostAsJsonAsync(
                "/api/products",
                new CreateProductCommand
                {
                    Name = sharedName,
                    SKU = "SHARED-SKU",
                    BaseUnitId = await GetUnitIdAsync(),
                    DefaultSellingPrice = 10,
                    CategoryId = category!.Id
                });
            productResponse.EnsureSuccessStatusCode();
            var product = await productResponse.Content
                .ReadFromJsonAsync<InventoryManagement.Application.Features.Products.CreateProduct.Response>();
            product.Should().NotBeNull();

            return new SeededMasterData(category.Id, customer!.Id, product!.Id);
        }

        private sealed record SeededMasterData(
            int CategoryId,
            int CustomerId,
            int ProductId);
    }
}
