using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Application.Features.Customers;
using InventoryManagement.Application.Features.Drivers;
using InventoryManagement.Application.Features.Purchases;
using InventoryManagement.Application.Features.SalesInvoices;
using InventoryManagement.Application.Features.Statements;
using InventoryManagement.Tests.IntegrationTests.Common;
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
using CurrentStockResponse = InventoryManagement.Application.Common.Models.PagedResponse<InventoryManagement.Application.Features.InventoryReports.GetCurrentStock.Response>;
using GrossProfitResponse = InventoryManagement.Application.Features.InventoryReports.GetGrossProfit.Response;
using ProductStockLedgerResponse = InventoryManagement.Application.Common.Models.PagedResponse<InventoryManagement.Application.Features.InventoryReports.GetProductStockLedger.Response>;
using PurchaseRegisterResponse = InventoryManagement.Application.Common.Models.RegisterResponse<InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister.Response>;
using SalesRegisterResponse = InventoryManagement.Application.Common.Models.RegisterResponse<InventoryManagement.Application.Features.InventoryReports.GetSalesRegister.Response>;

namespace InventoryManagement.Tests.IntegrationTests.TenantScope
{
    public class ReportTenantScopeTests : TestBase
    {
        public ReportTenantScopeTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Reports_Ledgers_And_Dashboard_Source_Apis_Should_Use_Active_Company()
        {
            await AuthenticateAsync();
            var companyAId = ActiveCompanyId;
            var companyA = await SeedReportDataAsync("A");
            var companyBId = await CreateAndSelectCompanyAsync();
            var companyB = await SeedReportDataAsync("B");

            SetActiveCompanyId(companyAId);

            var currentStock = await Client.GetFromJsonAsync<CurrentStockResponse>(
                "/api/inventory-reports/current-stock?pageNumber=1&pageSize=50");
            currentStock.Should().NotBeNull();
            currentStock!.Items.Select(x => x.ProductId).Should().Contain(companyA.ProductId);
            currentStock.Items.Select(x => x.ProductId).Should().NotContain(companyB.ProductId);

            var currentStockByOtherCategory = await Client.GetFromJsonAsync<CurrentStockResponse>(
                $"/api/inventory-reports/current-stock?pageNumber=1&pageSize=50&categoryId={companyB.CategoryId}");
            currentStockByOtherCategory.Should().NotBeNull();
            currentStockByOtherCategory!.Items.Should().BeEmpty();

            var purchaseRegister = await Client.GetFromJsonAsync<PurchaseRegisterResponse>(
                "/api/inventory-reports/purchase-register?pageNumber=1&pageSize=50");
            purchaseRegister.Should().NotBeNull();
            purchaseRegister!.Items.Select(x => x.PurchaseId).Should().Contain(companyA.PurchaseId);
            purchaseRegister.Items.Select(x => x.PurchaseId).Should().NotContain(companyB.PurchaseId);

            var purchaseRegisterByOtherSupplier = await Client.GetFromJsonAsync<PurchaseRegisterResponse>(
                $"/api/inventory-reports/purchase-register?pageNumber=1&pageSize=50&supplierId={companyB.SupplierId}");
            purchaseRegisterByOtherSupplier.Should().NotBeNull();
            purchaseRegisterByOtherSupplier!.Items.Should().BeEmpty();

            var salesRegister = await Client.GetFromJsonAsync<SalesRegisterResponse>(
                "/api/inventory-reports/sales-register?pageNumber=1&pageSize=50");
            salesRegister.Should().NotBeNull();
            salesRegister!.Items.Select(x => x.SalesInvoiceId).Should().Contain(companyA.SalesInvoiceId);
            salesRegister.Items.Select(x => x.SalesInvoiceId).Should().NotContain(companyB.SalesInvoiceId);

            var salesRegisterByOtherCustomer = await Client.GetFromJsonAsync<SalesRegisterResponse>(
                $"/api/inventory-reports/sales-register?pageNumber=1&pageSize=50&customerId={companyB.CustomerId}");
            salesRegisterByOtherCustomer.Should().NotBeNull();
            salesRegisterByOtherCustomer!.Items.Should().BeEmpty();

            var grossProfit = await Client.GetFromJsonAsync<GrossProfitResponse>(
                "/api/inventory-reports/gross-profit");
            grossProfit.Should().NotBeNull();
            grossProfit!.ByInvoice.Select(x => x.InvoiceId).Should().Contain(companyA.SalesInvoiceId);
            grossProfit.ByInvoice.Select(x => x.InvoiceId).Should().NotContain(companyB.SalesInvoiceId);

            var grossProfitByOtherProduct = await Client.GetFromJsonAsync<GrossProfitResponse>(
                $"/api/inventory-reports/gross-profit?productId={companyB.ProductId}");
            grossProfitByOtherProduct.Should().NotBeNull();
            grossProfitByOtherProduct!.ByProduct.Should().BeEmpty();
            grossProfitByOtherProduct.Summary.NetRevenue.Should().Be(0);

            var stockLedgerByOtherProduct = await Client.GetFromJsonAsync<ProductStockLedgerResponse>(
                $"/api/inventory-reports/products/{companyB.ProductId}/ledger?pageNumber=1&pageSize=50");
            stockLedgerByOtherProduct.Should().NotBeNull();
            stockLedgerByOtherProduct!.Items.Should().BeEmpty();

            var otherCustomerStatement = await Client.GetAsync(
                $"/api/customers/{companyB.CustomerId}/statement?dateFrom=2026-09-01&dateTo=2026-09-30");
            var otherSupplierStatement = await Client.GetAsync(
                $"/api/suppliers/{companyB.SupplierId}/statement?dateFrom=2026-09-01&dateTo=2026-09-30");
            var otherDriverDeliveries = await Client.GetAsync(
                $"/api/drivers/{companyB.DriverId}/deliveries?dateFrom=2026-09-01&dateTo=2026-09-30");

            otherCustomerStatement.StatusCode.Should().Be(HttpStatusCode.NotFound);
            otherSupplierStatement.StatusCode.Should().Be(HttpStatusCode.NotFound);
            otherDriverDeliveries.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var companyAStatement = await Client.GetFromJsonAsync<StatementResponse>(
                $"/api/customers/{companyA.CustomerId}/statement?dateFrom=2026-09-01&dateTo=2026-09-30");
            companyAStatement.Should().NotBeNull();
            companyAStatement!.Entries.Select(x => x.TransactionId).Should().Contain(companyA.SalesInvoiceId);

            var dashboardCustomers = await Client.GetFromJsonAsync<PagedResponse<CustomerResponse>>(
                "/api/customers?pageNumber=1&pageSize=50");
            var dashboardPurchases = await Client.GetFromJsonAsync<PagedResponse<PurchaseResponse>>(
                "/api/Purchases?pageNumber=1&pageSize=50");
            var dashboardSalesInvoices = await Client.GetFromJsonAsync<PagedResponse<SalesInvoiceResponse>>(
                "/api/sales-invoices?pageNumber=1&pageSize=50");

            dashboardCustomers.Should().NotBeNull();
            dashboardCustomers!.Items.Select(x => x.Id).Should().Contain(companyA.CustomerId);
            dashboardCustomers.Items.Select(x => x.Id).Should().NotContain(companyB.CustomerId);
            dashboardPurchases.Should().NotBeNull();
            dashboardPurchases!.Items.Select(x => x.Id).Should().Contain(companyA.PurchaseId);
            dashboardPurchases.Items.Select(x => x.Id).Should().NotContain(companyB.PurchaseId);
            dashboardSalesInvoices.Should().NotBeNull();
            dashboardSalesInvoices!.Items.Select(x => x.Id).Should().Contain(companyA.SalesInvoiceId);
            dashboardSalesInvoices.Items.Select(x => x.Id).Should().NotContain(companyB.SalesInvoiceId);

            SetActiveCompanyId(companyBId);
            var companyBStock = await Client.GetFromJsonAsync<CurrentStockResponse>(
                "/api/inventory-reports/current-stock?pageNumber=1&pageSize=50");
            companyBStock.Should().NotBeNull();
            companyBStock!.Items.Select(x => x.ProductId).Should().Contain(companyB.ProductId);
            companyBStock.Items.Select(x => x.ProductId).Should().NotContain(companyA.ProductId);
        }

        [Theory]
        [InlineData("/api/inventory-reports/current-stock")]
        [InlineData("/api/inventory-reports/purchase-register")]
        [InlineData("/api/inventory-reports/sales-register")]
        [InlineData("/api/inventory-reports/gross-profit")]
        [InlineData("/api/inventory-reports/products/1/ledger")]
        [InlineData("/api/customers/1/statement")]
        [InlineData("/api/suppliers/1/statement")]
        [InlineData("/api/drivers/1/deliveries")]
        public async Task Report_Endpoints_Should_Require_Company_Context(string path)
        {
            await AuthenticateAsync();
            Client.DefaultRequestHeaders.Remove("X-Company-Id");

            var response = await Client.GetAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private async Task<int> CreateAndSelectCompanyAsync()
        {
            var response = await Client.PostAsJsonAsync(
                "/api/companies",
                new CreateCompanyRequest
                {
                    Name = $"Report scope {Guid.NewGuid():N}"
                });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var company = await response.Content.ReadFromJsonAsync<UserCompanyDto>();
            company.Should().NotBeNull();
            SetActiveCompanyId(company!.Id);

            return company.Id;
        }

        private async Task<SeededReportData> SeedReportDataAsync(string prefix)
        {
            var suffix = Guid.NewGuid().ToString("N");

            var categoryResponse = await Client.PostAsJsonAsync(
                "/api/categories",
                new CreateCategoryCommand
                {
                    Name = $"{prefix} report category {suffix}",
                    Description = "Report tenant scope"
                });
            categoryResponse.EnsureSuccessStatusCode();
            var category = await categoryResponse.Content
                .ReadFromJsonAsync<InventoryManagement.Application.Features.Categories.Response>();
            category.Should().NotBeNull();

            var customerResponse = await Client.PostAsJsonAsync(
                "/api/customers",
                new CreateCustomerCommand
                {
                    Name = $"{prefix} report customer {suffix}",
                    CreditLimit = 1000
                });
            customerResponse.EnsureSuccessStatusCode();
            var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            customer.Should().NotBeNull();

            var supplierResponse = await Client.PostAsJsonAsync(
                "/api/suppliers",
                new CreateSupplierCommand
                {
                    Name = $"{prefix} report supplier {suffix}"
                });
            supplierResponse.EnsureSuccessStatusCode();
            var supplier = await supplierResponse.Content
                .ReadFromJsonAsync<InventoryManagement.Application.Features.Suppliers.SupplierResponse>();
            supplier.Should().NotBeNull();

            var driverResponse = await Client.PostAsJsonAsync(
                "/api/drivers",
                new CreateDriverCommand
                {
                    Name = $"{prefix} report driver {suffix}"
                });
            driverResponse.EnsureSuccessStatusCode();
            var driver = await driverResponse.Content.ReadFromJsonAsync<DriverResponse>();
            driver.Should().NotBeNull();

            var unitId = await GetUnitIdAsync("Ton");
            var productResponse = await Client.PostAsJsonAsync(
                "/api/products",
                new CreateProductCommand
                {
                    Name = $"{prefix} report product {suffix}",
                    SKU = $"{prefix}-REPORT-{suffix}",
                    BaseUnitId = unitId,
                    DefaultSellingPrice = 100,
                    CategoryId = category!.Id
                });
            productResponse.EnsureSuccessStatusCode();
            var product = await productResponse.Content
                .ReadFromJsonAsync<InventoryManagement.Application.Features.Products.CreateProduct.Response>();
            product.Should().NotBeNull();

            var purchaseResponse = await Client.PostAsJsonAsync(
                "/api/Purchases",
                new CreatePurchaseCommand
                {
                    PurchaseNumber = $"{prefix}-PUR-{suffix}",
                    SupplierId = supplier!.Id,
                    BillDate = new DateTime(2026, 9, 1),
                    Items =
                    {
                        new CreatePurchaseItemInput
                        {
                            ProductId = product!.Id,
                            Quantity = 10,
                            UnitCost = 30,
                            TaxRate = 0
                        }
                    }
                });
            purchaseResponse.EnsureSuccessStatusCode();
            var purchase = await purchaseResponse.Content.ReadFromJsonAsync<PurchaseResponse>();
            purchase.Should().NotBeNull();
            var postPurchase = await Client.PostAsync($"/api/Purchases/{purchase!.Id}/post", null);
            postPurchase.EnsureSuccessStatusCode();

            var invoiceResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new CreateSalesInvoiceCommand
                {
                    InvoiceNumber = $"{prefix}-INV-{suffix}",
                    CustomerId = customer!.Id,
                    DriverId = driver!.Id,
                    InvoiceDate = new DateTime(2026, 9, 2),
                    OtherCharges = 25,
                    LaborCharge = 10,
                    DeliveryAddress = $"{prefix} site",
                    Items =
                    {
                        new CreateSalesInvoiceItemInput
                        {
                            ProductId = product.Id,
                            Quantity = 2,
                            SellingUnitPrice = 75,
                            TaxRate = 0
                        }
                    }
                });
            invoiceResponse.EnsureSuccessStatusCode();
            var invoice = await invoiceResponse.Content.ReadFromJsonAsync<SalesInvoiceResponse>();
            invoice.Should().NotBeNull();
            var postInvoice = await Client.PostAsync($"/api/sales-invoices/{invoice!.Id}/post", null);
            postInvoice.EnsureSuccessStatusCode();

            return new SeededReportData(
                ActiveCompanyId,
                category.Id,
                customer.Id,
                supplier.Id,
                driver.Id,
                product.Id,
                purchase.Id,
                invoice.Id);
        }

        private sealed record SeededReportData(
            int CompanyId,
            int CategoryId,
            int CustomerId,
            int SupplierId,
            int DriverId,
            int ProductId,
            int PurchaseId,
            int SalesInvoiceId);
    }
}
