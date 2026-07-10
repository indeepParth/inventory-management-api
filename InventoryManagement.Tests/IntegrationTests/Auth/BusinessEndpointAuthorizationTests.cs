using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Features.Auth.Login;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class BusinessEndpointAuthorizationTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public BusinessEndpointAuthorizationTests(
            CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Business_Endpoints_Should_Return_Unauthorized_Without_Token()
        {
            var response = await Client.GetAsync("/api/products");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Admin_Should_Have_Full_Business_Access()
        {
            await AuthenticateWithRoleAsync(ApplicationRoles.Admin);

            var response = await Client.GetAsync("/api/purchases");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Manager_Should_Access_Cost_Reports_But_Not_User_Administration()
        {
            await AuthenticateWithRoleAsync(ApplicationRoles.Manager);

            var reportResponse = await Client.GetAsync(
                "/api/inventory-reports/current-stock");
            var usersResponse = await Client.GetAsync("/api/users");

            reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            usersResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Sales_Should_Read_Customers_And_Manage_Sales_But_Not_Purchases_Or_Cost_Reports()
        {
            await AuthenticateWithRoleAsync(ApplicationRoles.Sales);

            var productsResponse = await Client.GetAsync("/api/products");
            var customersResponse = await Client.GetAsync("/api/customers");
            var challansResponse = await Client.GetAsync("/api/delivery-challans");
            var invoicesResponse = await Client.GetAsync("/api/sales-invoices");
            var purchasesResponse = await Client.GetAsync("/api/purchases");
            var grossProfitResponse = await Client.GetAsync(
                "/api/inventory-reports/gross-profit");

            productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            customersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            challansResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            invoicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            purchasesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            grossProfitResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Sales_Should_Be_Authorized_To_Create_Customer_Receipts()
        {
            await AuthenticateWithRoleAsync(ApplicationRoles.Sales);

            var response = await Client.PostAsJsonAsync(
                "/api/payments",
                new
                {
                    ReceiptNumber = $"R-{Guid.NewGuid():N}",
                    CustomerId = 999999,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 10m,
                    Method = PaymentMethod.Cash
                });

            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Inventory_Should_Manage_Inventory_Flows_But_Not_Sales_Or_Customer_Reads()
        {
            await AuthenticateWithRoleAsync(ApplicationRoles.Inventory);

            var productsResponse = await Client.GetAsync("/api/products");
            var suppliersResponse = await Client.GetAsync("/api/suppliers");
            var purchasesResponse = await Client.GetAsync("/api/purchases");
            var stockMovementsResponse = await Client.GetAsync("/api/stock-movements");
            var customersResponse = await Client.GetAsync("/api/customers");
            var salesInvoicesResponse = await Client.GetAsync("/api/sales-invoices");

            productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            suppliersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            purchasesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            stockMovementsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            customersResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            salesInvoicesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Inventory_Should_Not_Access_Cost_Reports_Or_Manual_Adjustments()
        {
            await AuthenticateWithRoleAsync(ApplicationRoles.Inventory);

            var reportResponse = await Client.GetAsync(
                "/api/inventory-reports/current-stock");
            var adjustmentResponse = await Client.PostAsJsonAsync(
                "/api/stock-movements/adjustment",
                new { });

            reportResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            adjustmentResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData(ApplicationRoles.Sales, "/api/sales-invoices/1/cancel")]
        [InlineData(ApplicationRoles.Inventory, "/api/purchases/1/cancel")]
        public async Task Cancellation_Endpoints_Should_Be_Admin_Or_Manager_Only(
            string role,
            string path)
        {
            await AuthenticateWithRoleAsync(role);

            var response = await Client.PostAsync(path, null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private async Task AuthenticateWithRoleAsync(string role)
        {
            var user = await CreateUserAsync(role);

            var response = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new Command
                {
                    UserName = user.UserName,
                    Password = user.Password
                });

            response.EnsureSuccessStatusCode();

            var login = await response.Content.ReadFromJsonAsync<Response>();
            login.Should().NotBeNull();

            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    login!.AccessToken);
        }

        private async Task<TestUser> CreateUserAsync(string role)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var supportedRole in ApplicationRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(supportedRole))
                {
                    var roleResult = await roleManager.CreateAsync(
                        new IdentityRole(supportedRole));
                    roleResult.Succeeded.Should().BeTrue();
                }
            }

            var unique = Guid.NewGuid().ToString("N");
            var user = new ApplicationUser
            {
                UserName = $"policy_{unique}",
                Email = $"policy_{unique}@example.com"
            };
            const string password = "Password123";

            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();

            var addRoleResult = await userManager.AddToRoleAsync(user, role);
            addRoleResult.Succeeded.Should().BeTrue();

            return new TestUser(user.UserName!, password);
        }

        private sealed record TestUser(
            string UserName,
            string Password);
    }
}
