using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Application.Features.Customers;
using InventoryManagement.Application.Features.Drivers;
using InventoryManagement.Application.Features.Products.CreateProduct;
using InventoryManagement.Application.Features.Purchases;
using InventoryManagement.Application.Features.SalesInvoices;
using InventoryManagement.Application.Features.Suppliers;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CreateCategoryCommand = InventoryManagement.Application.Features.Categories.CreateCategory.Command;
using CreateCompanyRequest = InventoryManagement.API.Controllers.CreateCompanyRequest;
using CreateCustomerCommand = InventoryManagement.Application.Features.Customers.CreateCustomer.Command;
using CreateDriverCommand = InventoryManagement.Application.Features.Drivers.CreateDriver.Command;
using CreateProductCommand = InventoryManagement.Application.Features.Products.CreateProduct.Command;
using CreatePurchaseCommand = InventoryManagement.Application.Features.Purchases.CreatePurchase.Command;
using CreatePurchaseItemInput = InventoryManagement.Application.Features.Purchases.CreatePurchase.PurchaseItemInput;
using CreateSalesInvoiceCommand = InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice.Command;
using CreateSalesInvoiceItemInput = InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice.SalesInvoiceItemInput;
using CreateSupplierCommand = InventoryManagement.Application.Features.Suppliers.CreateSupplier.Command;
using ProductListResponse = InventoryManagement.Application.Common.Models.PagedResponse<InventoryManagement.Application.Features.Products.GetProducts.Response>;
using UpdateProductCommand = InventoryManagement.Application.Features.Products.UpdateProduct.Command;

namespace InventoryManagement.Tests.IntegrationTests.TenantScope
{
    public class AuthorizationHardeningTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthorizationHardeningTests(
            CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Active_Company_Should_Not_Read_Or_Mutate_Other_Company_Master_Data()
        {
            await AuthenticateAndCreateCompanyAsync();
            await SetActiveCompanyBillingPlanLimitsAsync(maxCompanies: 2);
            var companyAId = ActiveCompanyId;
            var companyA = await SeedMasterDataAsync("A");
            var companyBId = await CreateAndSelectCompanyAsync();
            var companyB = await SeedMasterDataAsync("B");

            SetActiveCompanyId(companyAId);

            var productGet = await Client.GetAsync($"/api/products/{companyB.ProductId}");
            var productUpdate = await Client.PutAsJsonAsync(
                $"/api/products/{companyB.ProductId}",
                new UpdateProductCommand(
                    companyB.ProductId,
                    $"Cross product {Guid.NewGuid():N}",
                    $"CROSS-{Guid.NewGuid():N}",
                    companyA.UnitId,
                    null,
                    null,
                    25,
                    companyA.CategoryId));
            var productDelete = await Client.DeleteAsync($"/api/products/{companyB.ProductId}");
            var customerGet = await Client.GetAsync($"/api/customers/{companyB.CustomerId}");
            var supplierGet = await Client.GetAsync($"/api/suppliers/{companyB.SupplierId}");
            var driverGet = await Client.GetAsync($"/api/drivers/{companyB.DriverId}");

            productGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
            productUpdate.StatusCode.Should().Be(HttpStatusCode.NotFound);
            productDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
            customerGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
            supplierGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
            driverGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

            SetActiveCompanyId(companyBId);
            var products = await Client.GetFromJsonAsync<ProductListResponse>("/api/products");
            products.Should().NotBeNull();
            products!.Items.Select(x => x.Id).Should().Contain(companyB.ProductId);
        }

        [Fact]
        public async Task Transaction_Create_Should_Reject_Cross_Company_Master_References()
        {
            await AuthenticateAndCreateCompanyAsync();
            await SetActiveCompanyBillingPlanLimitsAsync(maxCompanies: 2);
            var companyA = await SeedMasterDataAsync("A");
            await CreateAndSelectCompanyAsync();
            var companyB = await SeedMasterDataAsync("B");

            SetActiveCompanyId(companyA.CompanyId);

            var invoiceWithOtherCustomer = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new CreateSalesInvoiceCommand
                {
                    InvoiceNumber = $"INV-XC-CUST-{Guid.NewGuid():N}",
                    CustomerId = companyB.CustomerId,
                    InvoiceDate = new DateTime(2026, 9, 1),
                    Items =
                    {
                        new CreateSalesInvoiceItemInput
                        {
                            ProductId = companyA.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 10,
                            TaxRate = 0
                        }
                    }
                });

            var invoiceWithOtherProduct = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new CreateSalesInvoiceCommand
                {
                    InvoiceNumber = $"INV-XC-PROD-{Guid.NewGuid():N}",
                    CustomerId = companyA.CustomerId,
                    InvoiceDate = new DateTime(2026, 9, 1),
                    Items =
                    {
                        new CreateSalesInvoiceItemInput
                        {
                            ProductId = companyB.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 10,
                            TaxRate = 0
                        }
                    }
                });

            var purchaseWithOtherSupplier = await Client.PostAsJsonAsync(
                "/api/purchases",
                new CreatePurchaseCommand
                {
                    SupplierId = companyB.SupplierId,
                    BillDate = new DateTime(2026, 9, 1),
                    Items =
                    {
                        new CreatePurchaseItemInput
                        {
                            ProductId = companyA.ProductId,
                            Quantity = 1,
                            UnitCost = 10,
                            TaxRate = 0
                        }
                    }
                });

            var purchaseWithOtherProduct = await Client.PostAsJsonAsync(
                "/api/purchases",
                new CreatePurchaseCommand
                {
                    SupplierId = companyA.SupplierId,
                    BillDate = new DateTime(2026, 9, 1),
                    Items =
                    {
                        new CreatePurchaseItemInput
                        {
                            ProductId = companyB.ProductId,
                            Quantity = 1,
                            UnitCost = 10,
                            TaxRate = 0
                        }
                    }
                });

            invoiceWithOtherCustomer.StatusCode.Should().Be(HttpStatusCode.NotFound);
            invoiceWithOtherProduct.StatusCode.Should().Be(HttpStatusCode.NotFound);
            purchaseWithOtherSupplier.StatusCode.Should().Be(HttpStatusCode.NotFound);
            purchaseWithOtherProduct.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Active_Company_Should_Not_Mutate_Other_Company_Transactions()
        {
            await AuthenticateAndCreateCompanyAsync();
            await SetActiveCompanyBillingPlanLimitsAsync(maxCompanies: 2);
            var companyAId = ActiveCompanyId;
            await SeedMasterDataAsync("A");
            var companyBId = await CreateAndSelectCompanyAsync();
            var companyB = await SeedMasterDataAsync("B");
            var purchase = await CreatePurchaseAsync(companyB);
            var invoice = await CreateSalesInvoiceAsync(companyB);

            SetActiveCompanyId(companyAId);

            var getPurchase = await Client.GetAsync($"/api/purchases/{purchase.Id}");
            var postPurchase = await Client.PostAsync($"/api/purchases/{purchase.Id}/post", null);
            var cancelPurchase = await Client.PostAsync($"/api/purchases/{purchase.Id}/cancel", null);
            var getInvoice = await Client.GetAsync($"/api/sales-invoices/{invoice.Id}");
            var cancelInvoice = await Client.PostAsync($"/api/sales-invoices/{invoice.Id}/cancel", null);

            getPurchase.StatusCode.Should().Be(HttpStatusCode.NotFound);
            postPurchase.StatusCode.Should().Be(HttpStatusCode.NotFound);
            cancelPurchase.StatusCode.Should().Be(HttpStatusCode.NotFound);
            getInvoice.StatusCode.Should().Be(HttpStatusCode.NotFound);
            cancelInvoice.StatusCode.Should().Be(HttpStatusCode.NotFound);

            SetActiveCompanyId(companyBId);
            var purchaseStillVisible = await Client.GetAsync($"/api/purchases/{purchase.Id}");
            var invoiceStillVisible = await Client.GetAsync($"/api/sales-invoices/{invoice.Id}");
            purchaseStillVisible.StatusCode.Should().Be(HttpStatusCode.OK);
            invoiceStillVisible.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Active_Company_Profile_And_Members_Should_Not_Expose_Other_Company_Data()
        {
            await AuthenticateAndCreateCompanyAsync();
            await SetActiveCompanyBillingPlanLimitsAsync(maxCompanies: 2);
            var companyAId = ActiveCompanyId;
            await UpsertCompanyProfileAsync("Company A profile");
            var companyBId = await CreateAndSelectCompanyAsync();
            await UpsertCompanyProfileAsync("Company B profile");
            var companyBOnlyUser = await AddCompanyMemberAsync(companyBId);

            SetActiveCompanyId(companyAId);

            var profile = await Client.GetFromJsonAsync<CompanyProfileResponse>(
                "/api/company-profile");
            var users = await Client.GetFromJsonAsync<List<UserManagementResponse>>(
                "/api/users");

            profile.Should().NotBeNull();
            profile!.CompanyName.Should().Be("Company A profile");
            users.Should().NotBeNull();
            users!.Select(x => x.Id).Should().NotContain(companyBOnlyUser.Id);
        }

        private async Task<int> CreateAndSelectCompanyAsync()
        {
            var response = await Client.PostAsJsonAsync(
                "/api/companies",
                new CreateCompanyRequest
                {
                    Name = $"Tenant hardening {Guid.NewGuid():N}"
                });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var company = await response.Content.ReadFromJsonAsync<UserCompanyDto>();
            company.Should().NotBeNull();
            SetActiveCompanyId(company!.Id);

            return company.Id;
        }

        private async Task<SeededData> SeedMasterDataAsync(string prefix)
        {
            var categoryResponse = await Client.PostAsJsonAsync(
                "/api/categories",
                new CreateCategoryCommand
                {
                    Name = $"{prefix} category {Guid.NewGuid():N}",
                    Description = "Tenant security category"
                });
            categoryResponse.EnsureSuccessStatusCode();
            var category = await categoryResponse.Content
                .ReadFromJsonAsync<InventoryManagement.Application.Features.Categories.Response>();
            category.Should().NotBeNull();

            var customerResponse = await Client.PostAsJsonAsync(
                "/api/customers",
                new CreateCustomerCommand
                {
                    Name = $"{prefix} customer {Guid.NewGuid():N}",
                    CreditLimit = 1000
                });
            customerResponse.EnsureSuccessStatusCode();
            var customer = await customerResponse.Content
                .ReadFromJsonAsync<CustomerResponse>();
            customer.Should().NotBeNull();

            var supplierResponse = await Client.PostAsJsonAsync(
                "/api/suppliers",
                new CreateSupplierCommand
                {
                    Name = $"{prefix} supplier {Guid.NewGuid():N}"
                });
            supplierResponse.EnsureSuccessStatusCode();
            var supplier = await supplierResponse.Content
                .ReadFromJsonAsync<SupplierResponse>();
            supplier.Should().NotBeNull();

            var driverResponse = await Client.PostAsJsonAsync(
                "/api/drivers",
                new CreateDriverCommand
                {
                    Name = $"{prefix} driver {Guid.NewGuid():N}"
                });
            driverResponse.EnsureSuccessStatusCode();
            var driver = await driverResponse.Content
                .ReadFromJsonAsync<DriverResponse>();
            driver.Should().NotBeNull();

            var unitId = await GetUnitIdAsync();
            var productResponse = await Client.PostAsJsonAsync(
                "/api/products",
                new CreateProductCommand
                {
                    Name = $"{prefix} product {Guid.NewGuid():N}",
                    SKU = $"{prefix}-SKU-{Guid.NewGuid():N}",
                    BaseUnitId = unitId,
                    DefaultSellingPrice = 50,
                    CategoryId = category!.Id
                });
            productResponse.EnsureSuccessStatusCode();
            var product = await productResponse.Content
                .ReadFromJsonAsync<InventoryManagement.Application.Features.Products.CreateProduct.Response>();
            product.Should().NotBeNull();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var productEntity = await db.Products.SingleAsync(x => x.Id == product!.Id);
            productEntity.Quantity = 100;
            await db.SaveChangesAsync();

            return new SeededData(
                ActiveCompanyId,
                category.Id,
                customer!.Id,
                supplier!.Id,
                driver!.Id,
                unitId,
                product.Id);
        }

        private async Task<PurchaseResponse> CreatePurchaseAsync(SeededData seed)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/purchases",
                new CreatePurchaseCommand
                {
                    SupplierId = seed.SupplierId,
                    BillDate = new DateTime(2026, 9, 2),
                    Items =
                    {
                        new CreatePurchaseItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 2,
                            UnitCost = 20,
                            TaxRate = 0
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            var purchase = await response.Content.ReadFromJsonAsync<PurchaseResponse>();
            purchase.Should().NotBeNull();

            return purchase!;
        }

        private async Task<SalesInvoiceResponse> CreateSalesInvoiceAsync(
            SeededData seed)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new CreateSalesInvoiceCommand
                {
                    InvoiceNumber = $"INV-TENANT-{Guid.NewGuid():N}",
                    CustomerId = seed.CustomerId,
                    InvoiceDate = new DateTime(2026, 9, 2),
                    Items =
                    {
                        new CreateSalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 30,
                            TaxRate = 0
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            var invoice = await response.Content.ReadFromJsonAsync<SalesInvoiceResponse>();
            invoice.Should().NotBeNull();

            return invoice!;
        }

        private async Task UpsertCompanyProfileAsync(string companyName)
        {
            var response = await Client.PutAsJsonAsync(
                "/api/company-profile",
                new
                {
                    CompanyName = companyName
                });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private async Task<ApplicationUser> AddCompanyMemberAsync(int companyId)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unique = Guid.NewGuid().ToString("N");
            var user = new ApplicationUser
            {
                UserName = $"tenant_member_{unique}",
                Email = $"tenant_member_{unique}@example.com"
            };

            var result = await userManager.CreateAsync(user, "Password123");
            result.Succeeded.Should().BeTrue();
            db.CompanyUsers.Add(new CompanyUser
            {
                CompanyId = companyId,
                UserId = user.Id,
                Role = "Viewer",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            return user;
        }

        private sealed record SeededData(
            int CompanyId,
            int CategoryId,
            int CustomerId,
            int SupplierId,
            int DriverId,
            int UnitId,
            int ProductId);

        private sealed class CompanyProfileResponse
        {
            public string CompanyName { get; set; } = string.Empty;
        }

        private sealed class UserManagementResponse
        {
            public string Id { get; set; } = string.Empty;
        }
    }
}
